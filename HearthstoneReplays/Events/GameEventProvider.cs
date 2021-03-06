﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HearthstoneReplays.Parser;
using System.Text.RegularExpressions;

namespace HearthstoneReplays.Events
{
    public class GameEventProvider
    {
        public Func<GameEvent> SupplyGameEvent { get; set; }
        public Func<GameEventProvider, bool> isDuplicatePredicate { get; set; }
        public DateTime Timestamp { get; set; }
        public bool NeedMetaData { get; set; }
        public bool AnimationReady { get; set; }
        public string EventName{ get; set; }
        public string CreationLogLine { get; set; }
        public GameEvent GameEvent { get; private set; }

        private Helper helper = new Helper();

        public bool debug { get; set; }

        private GameEventProvider()
        {

        }

        public static GameEventProvider Create(
            DateTime originalTimestamp,
            string eventName, // Used to give a "type" to the provider
            Func<GameEvent> eventProvider,
            bool needMetaData, 
            string creationLogLine,
            bool animationReady = false,
            bool debug = false)
        {
            if (animationReady)
            {
                Logger.Log("Creating event with animation ready", creationLogLine);
            }
            return Create(originalTimestamp, eventName, eventProvider, (a) => false, needMetaData, creationLogLine, animationReady, debug);
        }

        public static GameEventProvider Create(
            DateTime originalTimestamp,
            string eventName,
            Func<GameEvent> eventProvider,
            Func<GameEventProvider, bool> isDuplicatePredicate,
            bool needMetaData,
            string creationLogLine,
            bool animationReady = false,
            bool debug = false)
        {
            var result = new GameEventProvider
            {
                Timestamp = originalTimestamp,
                EventName = eventName,
                SupplyGameEvent = eventProvider,
                isDuplicatePredicate = isDuplicatePredicate,
                NeedMetaData = needMetaData,
                CreationLogLine = creationLogLine,
                debug = debug,
            };
            result.AnimationReady = animationReady;
            if (debug)
            {
                Logger.Log("Creating game event provider in debug " + result.debug, creationLogLine);
            }
            return result;
        }

        public bool ReceiveAnimationLog(string data, ParserState state)
        {
            if (GameEvent != null)
            {
                return false;
            }
            if (debug)
            {
                Logger.Log("\nReceiving anomation log " + data, debug);
            }
            // Mark the event as ready to be emitted
            IsEventReady(data, state);
            // And now's the time to compute the event itself
            if (AnimationReady && GameEvent == null)
            {
                if (debug)
                {
                    Logger.Log("IsEventReady, supplying game event", "");
                }
                GameEvent = SupplyGameEvent();
                return true;
            }
            return false;
        }

        private void IsEventReady(string data, ParserState state)
        {
            if (CreationLogLine == null)
            {
                Logger.Log("Error Missing CreationLogLine for ", data);
            }
            data = data.Trim();
            if (debug)
            {
                Logger.Log("IsEventReady, data", data + " // " + CreationLogLine);
            }

            if (data == CreationLogLine)
            {
                if (debug)
                {
                    Logger.Log("IsEventReady, AnimationReady", "animation ready");
                }
                AnimationReady = true;
                return;
            }
            // In the case of discarded cards, the position in the zone can change between 
            // the GameState and PowerTaskList logs, so we do a check without taking 
            // the zone position into account
            var dataWithoutZones = Regex.Replace(data, @"zonePos=\d", "");
            var creationLogWithoutZones = Regex.Replace(CreationLogLine, @"zonePos=\d", "");
            if (dataWithoutZones == creationLogWithoutZones)
            {
                if (debug)
                {
                    Logger.Log("IsEventReady, AnimationReady without zones", "animation ready");
                }
                AnimationReady = true;
                return;
            }

            // And sometimes the full entity is logged in PTL, while only the entity is logged 
            // in GS
            var ptlMatchForFullEntity = Regexes.EntityRegex.Match(data);
            if (ptlMatchForFullEntity.Success)
            {
                var ptlId = ptlMatchForFullEntity.Groups[1];
                var dataWithOnlyEntityId = Regex.Replace(data, Regexes.EntityRegex.ToString(), "" + ptlId);
                if (dataWithOnlyEntityId == CreationLogLine)
                {
                    if (debug)
                    {
                        Logger.Log("IsEventReady, AnimationReady with only entity id", "animation ready");
                    }
                    AnimationReady = true;
                    return;
                } 
                // Patches the Pirate for instance doesn't log the cardId in the GS, but does in PTL
                else
                {
                    var gsMatchForFullEntity = Regexes.EntityRegex.Match(CreationLogLine);
                    if (gsMatchForFullEntity.Success)
                    {
                        var gsId = gsMatchForFullEntity.Groups[1];
                        var gsDataWithOnlyEntityId = Regex.Replace(CreationLogLine, Regexes.EntityRegex.ToString(), "" + gsId);
                        if (dataWithOnlyEntityId == gsDataWithOnlyEntityId)
                        {
                            if (debug)
                            {
                                Logger.Log("IsEventReady, AnimationReady with only entity id gsDataWithOnlyEntityId " + dataWithOnlyEntityId + " // " + gsDataWithOnlyEntityId,
                                    CreationLogLine + " // " + data);
                            }
                            AnimationReady = true;
                            return;
                        }
                    }
                }
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

                //Logger.Log("comparing " + ptlRawEntity, gsRawEntity);
                if (gsEntity == ptlEntity)
                {
                    if (debug)
                    {
                        Logger.Log("IsEventReady, AnimationReady with entity check", "animation ready");
                    }
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
                    if (debug)
                    {
                        Logger.Log("IsEventReady, AnimationReady with second entity check", "animation ready");
                    }
                    AnimationReady = true;
                    return;
                }
            }
        }
    }
}
