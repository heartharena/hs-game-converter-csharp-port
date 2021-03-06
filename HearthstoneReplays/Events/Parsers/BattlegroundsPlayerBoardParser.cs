﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;
using System;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsPlayerBoardParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsPlayerBoardParser(ParserState ParserState)
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
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && GameState.GetGameEntity().GetTag(GameTag.TURN) % 2 == 0
                && node.Type == typeof(Parser.ReplayData.GameActions.Action)
                && (node.Object as Parser.ReplayData.GameActions.Action).Type == (int)BlockType.TRIGGER
                && (node.Object as Parser.ReplayData.GameActions.Action).EffectIndex == -1;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            var entity = GameState.CurrentEntities[action.Entity];
            if (entity.CardId != "TB_BaconShop_8P_PlayerE")
            {
                return null;
            }

            var hasHero = action.Data
                .Where(data => data.GetType() == typeof(FullEntity))
                .Select(data => data as FullEntity)
                .Any(data => data.CardId?.Contains("HERO") ?? false);
            if (!hasHero)
            {
                return null;
            }

            var opponent = ParserState.OpponentPlayer;
            var player = ParserState.LocalPlayer;
            return new List<GameEventProvider> { CreateProviderFromAction(node, player), CreateProviderFromAction(node, opponent) };
        }

        private GameEventProvider CreateProviderFromAction(Node node, Player player)
        {
            var action = node.Object as Parser.ReplayData.GameActions.Action;
            var heroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .ToList();
            var potentialHeroes = GameState.CurrentEntities.Values
                .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                // Here we accept to face the ghost
                .Where(entity => entity.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl)
                .ToList();
            var hero = potentialHeroes
                //.Where(entity => entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                .FirstOrDefault();
            //Logger.Log("Trying to handle board", "" + ParserState.CurrentGame.GameType + " // " + hero?.CardId);
            //Logger.Log("Hero " + hero.CardId, hero.Entity);
            var cardId = hero?.CardId;
            if (cardId == NonCollectible.Neutral.KelthuzadTavernBrawl2)
            {
                //Logger.Log("Fighting the ghost", "Trying to assign the previous card id");
                // Take the last one
                var deadHero = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO)
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl)
                    .Where(entity => entity.CardId != NonCollectible.Neutral.KelthuzadTavernBrawl2)
                    .OrderBy(entity => entity.Id)
                    .LastOrDefault();
                cardId = deadHero?.CardId;
            }
            // Happens in the first encounter
            if (cardId == null)
            {
                var activePlayer = GameState.CurrentEntities[ParserState.LocalPlayer.Id];
                var opponentPlayerId = activePlayer.GetTag(GameTag.NEXT_OPPONENT_PLAYER_ID);
                hero = GameState.CurrentEntities.Values
                    .Where(data => data.GetTag(GameTag.PLAYER_ID) == opponentPlayerId)
                    .FirstOrDefault();
                cardId = hero?.CardId;
            }
            if (cardId != null)
            {
                // We don't use the game state builder here because we really need the full entities
                var board = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.MINION)
                    .OrderBy(entity => entity.GetTag(GameTag.ZONE_POSITION))
                    .Select(entity => entity.Clone())
                    .ToList();
                var heroPower = GameState.CurrentEntities.Values
                    .Where(entity => entity.GetTag(GameTag.CONTROLLER) == player.PlayerId)
                    .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                    .Where(entity => entity.GetTag(GameTag.CARDTYPE) == (int)CardType.HERO_POWER)
                    .Select(entity => entity.Clone())
                    .FirstOrDefault();
                var result = board.Select(entity => AddEchantments(GameState.CurrentEntities, entity)).ToList();
                //Logger.Log("board has " + board.Count + " entities", "");
                return GameEventProvider.Create(
                   action.TimeStamp,
                   "BATTLEGROUNDS_PLAYER_BOARD",
                   () => (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS 
                            || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                        ? new GameEvent
                        {
                            Type = "BATTLEGROUNDS_PLAYER_BOARD",
                            Value = new
                            {
                                Hero = hero,
                                HeroPowerCardId = heroPower?.CardId,
                                HeroPowerUsed = heroPower?.GetTag(GameTag.EXHAUSTED) == 1 || heroPower?.GetTag(GameTag.PENDING_TRIGGER) == 1,
                                CardId = cardId,
                                Board = result,
                            }
                        }
                        : null,
                   true,
                   node.CreationLogLine);
            }
            //else
            //{
            //    Logger.Log("Invalid hero", hero != null ? hero.CardId : "null hero");
            //}
            return null;
        }

        private object AddEchantments(Dictionary<int, FullEntity> currentEntities, FullEntity fullEntity)
        {
            var enchantments = currentEntities.Values
                .Where(entity => entity.GetTag(GameTag.ATTACHED) == fullEntity.Id)
                .Where(entity => entity.GetTag(GameTag.ZONE) == (int)Zone.PLAY)
                .Select(entity => new
                {
                    EntityId = entity.Id,
                    CardId = entity.CardId
                })
                .ToList();
            //var test = currentEntities.Values
            //    .Where(entity => entity.GetTag(GameTag.ATTACHED) > 0)
            //    .Select(entity => entity.CardId)
            //    .ToList();
            //if (test.Count > 0)
            //{
            //    Logger.Log("Eeenchantments", test);
            //}
            dynamic result = new
            {
                CardId = fullEntity.CardId,
                Entity = fullEntity.Entity,
                Id = fullEntity.Id,
                Tags = fullEntity.Tags,
                TimeStamp = fullEntity.TimeStamp,
                Enchantments = enchantments,
            };
            return result;
        }
    }
}
