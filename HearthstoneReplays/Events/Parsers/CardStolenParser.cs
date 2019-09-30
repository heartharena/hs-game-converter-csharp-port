using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardStolenParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardStolenParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.CONTROLLER;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(Parser.ReplayData.GameActions.ShowEntity)
                && GameState.CurrentEntities[(node.Object as ShowEntity).Entity].GetTag(GameTag.CONTROLLER) 
                        != (node.Object as ShowEntity).GetTag(GameTag.CONTROLLER);
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            var tagChange = node.Object as TagChange;
            var entity = GameState.CurrentEntities[tagChange.Entity];
            var cardId = entity.CardId;
            var controllerId = entity.GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[tagChange.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var gameState = GameEvent.BuildGameState(ParserState, GameState);
                return new List<GameEventProvider> { GameEventProvider.Create(
                    tagChange.TimeStamp,
                    GameEvent.CreateProvider(
                        "CARD_STOLEN",
                        cardId,
                        controllerId,
                        entity.Id,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            newControllerId = tagChange.Value
                        }),
                    // A "stolen" event can first remove the card from the deck before changing the controller (
                    // (in two separate steps), which is why we remove it
                    (GameEventProvider provider) =>
                        {
                        if (provider == null)
                        {
                            Logger.Log("Error: trying to instantiate an event with null provider", node.CreationLogLine);
                            return false;
                        }
                        var gameEvent = provider.SupplyGameEvent();
                        if (gameEvent == null)
                        {
                            Logger.Log("Could not identify gameEvent", provider.CreationLogLine);
                            return false;
                        }
                        if (gameEvent.Type == "CARD_REMOVED_FROM_DECK")
                        {
                            dynamic obj = gameEvent.Value;
                            return obj != null && (int)obj.EntityId == tagChange.Entity;
                        }
                        else if (gameEvent.Type == "RECEIVE_CARD_IN_HAND")
                        {
                            dynamic obj = gameEvent.Value;
                            return obj != null && (int)obj.EntityId == tagChange.Entity;
                        }
                        return false;
                    },
                    true,
                    node.CreationLogLine) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var showEntity = node.Object as Parser.ReplayData.GameActions.ShowEntity;
            var cardId = showEntity.CardId;
            var controllerId = GameState.CurrentEntities[showEntity.Entity].GetTag(GameTag.CONTROLLER);
            if (GameState.CurrentEntities[showEntity.Entity].GetTag(GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
            {
                var gameState = GameEvent.BuildGameState(ParserState, GameState);
                return new List<GameEventProvider> { GameEventProvider.Create(
                    showEntity.TimeStamp,
                    GameEvent.CreateProvider(
                        "CARD_STOLEN",
                        cardId,
                        controllerId,
                        showEntity.Entity,
                        ParserState,
                        GameState,
                        gameState,
                        new {
                            newControllerId = showEntity.GetTag(GameTag.CONTROLLER)
                        }),
                    (GameEventProvider provider) =>
                        { 
                        if (provider == null)
                        {
                            Logger.Log("Error: trying to instantiate an event with null provider", node.CreationLogLine);
                            return false;
                        }
                        var gameEvent = provider.SupplyGameEvent();
                        if (gameEvent == null)
                        {
                            Logger.Log("Could not identify gameEvent", provider.CreationLogLine);
                            return false;
                        }
                        if (gameEvent.Type != "CARD_REMOVED_FROM_DECK")
                        {
                            return false;
                        }
                        dynamic obj = gameEvent.Value;
                        return obj != null && (int)obj.EntityId == showEntity.Entity;
                    },
                    true,
                    node.CreationLogLine) };
            }
            return null;
        }
    }
}
