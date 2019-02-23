﻿using HearthstoneReplays.Parser;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using System;
using HearthstoneReplays.Enums;
using HearthstoneReplays.Parser.ReplayData.Entities;

namespace HearthstoneReplays.Events.Parsers
{
    public class CardPresentOnGameStartParser : ActionParser
    {
        private GameState GameState { get; set; }
        private ParserState ParserState { get; set; }

        public CardPresentOnGameStartParser(ParserState ParserState)
        {
            this.ParserState = ParserState;
            this.GameState = ParserState.GameState;
        }

        public bool NeedMetaData()
        {
            return true;
        }

        public bool AppliesOnNewNode(Node node)
        {
            return false;
        }

        public bool AppliesOnCloseNode(Node node)
        {
            return node.Type == typeof(FullEntity)
                && (node.Object as FullEntity).GetTag(GameTag.ZONE) == (int)Zone.PLAY
                && node.Parent != null && node.Parent.Type == typeof(Game);
        }

        public GameEventProvider CreateGameEventProviderFromNew(Node node)
        {
            return null;
        }

        public GameEventProvider CreateGameEventProviderFromClose(Node node)
        {
            var fullEntity = node.Object as FullEntity;
            return new GameEventProvider
            {
                Timestamp = DateTimeOffset.Parse(fullEntity.TimeStamp),
                GameEvent = new GameEvent
                {
                    Type = "CARD_ON_BOARD_AT_GAME_START",
                    Value = new
                    {
                        CardId = fullEntity.CardId,
                        ControllerId = fullEntity.GetTag(GameTag.CONTROLLER),
                        LocalPlayer = ParserState.LocalPlayer,
                        OpponentPlayer = ParserState.OpponentPlayer
                    }
                }
            };
        }
    }
}
