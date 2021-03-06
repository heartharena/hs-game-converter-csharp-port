﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardPlayedFromHandParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardPlayedFromHandParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.ZONE
                && (node.Object as TagChange).Value == (int)Zone.PLAY
                && GameState.CurrentEntities[(node.Object as TagChange).Entity].GetTag(GameTag.ZONE) == (int)Zone.HAND;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.Action) 
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.PLAY;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var targetId = -1;
                string targetCardId = null;
                if (node.Parent.Type == typeof(Parser.ReplayData.GameActions.Action))
                {
                    var action = node.Parent.Object as Parser.ReplayData.GameActions.Action;
                    targetId = action.Target;
                    targetCardId = targetId > 0 ? GameState.CurrentEntities[targetId].CardId : null;
                }
                var gameState = GameEvent.BuildGameState(ParserState, GameState, tagChange, null);
                return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    "CARD_PLAYED",
                    GameEvent.CreateProvider(
                        "CARD_PLAYED",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            TargetEntityId = targetId,
                            TargetCardId = targetCardId,
                            Attack = entity.GetTag(GameTag.ATK),
                            Health = entity.GetTag(GameTag.HEALTH),
                        }),
                    true,
                    node.CreationLogLine) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            foreach (var data in action.Data) {
                if (data.GetType() == typeof(ShowEntity))
                {
                    var showEntity = data as ShowEntity;
                    if (showEntity.GetTag(GameTag.ZONE) == (int)Zone.PLAY 
                        && showEntity.GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
                    {
                        var cardId = showEntity.CardId;
                        var controllerId = showEntity.GetTag(GameTag.CONTROLLER);
                        var gameState = GameEvent.BuildGameState(ParserState, GameState, null, showEntity);
                        var targetId = action.Target;
                        string targetCardId = targetId > 0 ? GameState.CurrentEntities[targetId].CardId : null;
                        // For now there can only be one card played per block
                        return new List<GameEventProvider> { GameEventProvider.Create(
                            action.TimeStamp,
                            "CARD_PLAYED",
                            GameEvent.CreateProvider(
                                "CARD_PLAYED",
                                cardId,
                                controllerId,
                                showEntity.Entity,
                                ParserState,
                                GameState,
                                gameState,
                                new {
                                    TargetEntityId = targetId,
                                    TargetCardId = targetCardId,
                                }),
                            true,
                            node.CreationLogLine) };
                    }
                }
            }
            return null;
        }
    }
}
