﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System.Linq;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;
using System.Collections.Generic;
using static HearthstoneReplays.Events.CardIds;

namespace HearthstoneReplays.Events.Parsers
{
    public class BattlegroundsPlayerTechLevelUpdatedParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public BattlegroundsPlayerTechLevelUpdatedParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return (ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS
                    || ParserState.CurrentGame.GameType == (int)GameType.GT_BATTLEGROUNDS_FRIENDLY)
                && node.Type == typeof(TagChange)
                && (node.Object as TagChange).Name == (int)GameTag.PLAYER_TECH_LEVEL;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return false;
        }

        public List<GameEventProvider> CreateGameEventProviderFromNew(Node node)
        {
            if (node.Parent != null && node.Parent.Type == typeof(Action))
            {
                var parent = node.Parent.Object as Action;
                if (parent.Type == (int)BlockType.TRIGGER)
                {
                    return null;
                }
            }
            var tagChange = node.Object as TagChange;
            var hero = GameState.CurrentEntities[tagChange.Entity];
            // The value is set to 0 when rotating the entities it seems
            if (hero?.CardId != null && hero.CardId != NonCollectible.Neutral.BobsTavernTavernBrawl && tagChange.Value > 1)
            {
                return new List<GameEventProvider> {  GameEventProvider.Create(
               tagChange.TimeStamp,
               "BATTLEGROUNDS_TAVERN_UPGRADE",
               () => new GameEvent
               {
                   Type = "BATTLEGROUNDS_TAVERN_UPGRADE",
                   Value = new
                   {
                       CardId = hero.CardId,
                       TavernLevel = tagChange.Value,
                   }
               },
               false,
               node.CreationLogLine) };
            }
            return null;
        }

        public List<GameEventProvider> CreateGameEventProviderFromClose(Node node)
        {
            return null;
        }
    }
}
