﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardChangedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardChangedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(ChangeEntity);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var changeEntity = node.Object as ChangeEntity;
            string eventName = null;
            if (GameState.CurrentEntities[changeEntity.Entity].GetTag(GameTag.ZONE) == (int)Zone.PLAY)
            {
                eventName = "CARD_CHANGED_ON_BOARD";
            }
            else if (GameState.CurrentEntities[changeEntity.Entity].GetTag(GameTag.ZONE) == (int)Zone.HAND)
            {
                eventName = "CARD_CHANGED_IN_HAND";
            }
            else if (GameState.CurrentEntities[changeEntity.Entity].GetTag(GameTag.ZONE) == (int)Zone.DECK)
            {
                eventName = "CARD_CHANGED_IN_DECK";
            }
            if (eventName == null)
            {
                return null;
            }
            var cardId = changeEntity.CardId;
            var entity = GameState.CurrentEntities[changeEntity.Entity];
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            var gameState = GameEvent.BuildGameState(ParserState, GameState, null, null);
            var creatorEntityId = changeEntity.GetTag(GameTag.CREATOR);
            var creatorEntityCardId = GameState.CurrentEntities.ContainsKey(creatorEntityId)
                ? GameState.CurrentEntities[creatorEntityId].CardId
                : null;
            return new List<GameEventProvider> { GameEventProvider.Create(
                changeEntity.TimeStamp,
                eventName,
                GameEvent.CreateProvider(
                    eventName,
                    cardId,
                    controllerId,
                    entity.Id,
                    ParserState,
                    GameState,
                    gameState,
                    new {
                        CreatorCardId = creatorEntityCardId,
                    }),
                true,
                node.CreationLogLine) };
        }
    }
}
