﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Parser;

namespace HearthstoneReplays.Events
{
    public class GameEventProvider
    {
        public Func<GameEvent> SupplyGameEvent { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public bool NeedMetaData { get; set; }
        public bool AnimationReady { get; set; }
        public string CreationLogLine { get; set; }

        private Helper helper = new Helper();

        private GameEventProvider()
        {

        }

        public static GameEventProvider Create(
            string originalTimestamp, 
            Func<GameEvent> eventProvider, 
            bool needMetaData,
            string creationLogLine)
        {
            return new GameEventProvider
            {
                Timestamp = ParseTimestamp(originalTimestamp),
                SupplyGameEvent = eventProvider,
                NeedMetaData = needMetaData,
                CreationLogLine = creationLogLine
            };
        }

        public void ReceiveAnimationLog(string data, ParserState state)
        {
            if (CreationLogLine == null)
            {
                Console.WriteLine("ERROR - Missing CreationLogLine for " + SupplyGameEvent);
            }
            data = data.Trim();
            if (data == CreationLogLine)
            {
                AnimationReady = true;
                return;
            }
            // Sometimes the information doesn't exactly match - one has more details on the entity
            // So here we compared the most basic form of both logs
            var matchShowInGameState = Regexes.ActionShowEntityRegex.Match(CreationLogLine);
            var matchShowInPowerTaskList = Regexes.ActionShowEntityRegex.Match(data);
            if (matchShowInGameState.Success && matchShowInPowerTaskList.Success)
            {
                var gsRawEntity = matchShowInGameState.Groups[1].Value;
                var gsEntity = helper.ParseEntity(gsRawEntity, state);

                var ptlRawEntity = matchShowInGameState.Groups[1].Value;
                var ptlEntity = helper.ParseEntity(ptlRawEntity, state);

                if (gsEntity == ptlEntity)
                {
                    AnimationReady = true;
                    return;
                }
            }
            // Special case for PowerTaskList Updating an entity that was only created in GameState
            var matchCreationInGameState = Regexes.ActionFullEntityCreatingRegex.Match(CreationLogLine);
            var matchUpdateInPowerTaskList = Regexes.ActionFullEntityUpdatingRegex.Match(data);
            if (matchCreationInGameState.Success && matchUpdateInPowerTaskList.Success)
            {
                var gsRawEntity = matchCreationInGameState.Groups[1].Value;
                var gsEntity = helper.ParseEntity(gsRawEntity, state);

                var ptlRawEntity = matchUpdateInPowerTaskList.Groups[1].Value;
                var ptlEntity = helper.ParseEntity(ptlRawEntity, state);

                if (gsEntity == ptlEntity)
                {
                    AnimationReady = true;
                    return;
                }
            }
        }

        private static DateTimeOffset ParseTimestamp(string timestamp)
        {
            if (!string.IsNullOrEmpty(timestamp))
            {
                String[] split = timestamp.Split(':');
                int hours = int.Parse(split[0]);
                if (hours >= 24)
                {
                    String newTs = "00:" + split[1] + ":" + split[2];
                    return DateTimeOffset.Parse(newTs).AddDays(1);
                }
                return DateTimeOffset.Parse(timestamp);
            }
            return DateTimeOffset.MinValue;
        }
    }
}
