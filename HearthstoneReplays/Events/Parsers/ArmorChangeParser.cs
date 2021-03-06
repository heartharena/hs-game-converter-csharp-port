﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class ArmorChangeParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public ArmorChangeParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ARMOR;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var initialArmor = entity.GetTag(GameTag.ARMOR);
            var newArmor = tagChange.Value;
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            return new List<GameEventProvider> { GameEventProvider.Create(
                tagChange.TimeStamp,
                 "ARMOR_CHANGED",
                () => new GameEvent
                {
                    Type = "ARMOR_CHANGED",
                    Value = new
                    {
                        CardId = cardId,
                        ControllerId = controllerId,
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer,
                        ArmorChange = newArmor - initialArmor,
                        TotalArmor = newArmor,
                        EntityId = entity.Id, // Might be useful if we want to uniquely identify the minion
                    }
                },
                true,
                node.CreationLogLine) };
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
