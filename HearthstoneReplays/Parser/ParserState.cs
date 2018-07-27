#region

using System;
using System.Collections.Generic;
using System.Linq;
using HearthstoneReplays.Parser.ReplayData;
using HearthstoneReplays.Parser.ReplayData.GameActions;
using HearthstoneReplays.Parser.ReplayData.Meta;
using HearthstoneReplays.Parser.ReplayData.Meta.Options;
using HearthstoneReplays.Parser.ReplayData.Entities;
using HearthstoneReplays.Enums;

#endregion

namespace HearthstoneReplays.Parser
{
	public class ParserState
	{
		public Action<object> EventHandler { get; set; }

		public ParserState()
		{
			FirstPlayerId = -1;
			Reset();
		}

		public HearthstoneReplay Replay { get; set; }
		public Game CurrentGame { get; set; }
		public Node Node { get; set; }
		public GameData GameData { get; set; }
		public SendChoices SendChoices { get; set; }
		public Choices Choices { get; set; }
		public Options Options { get; set; }
		public Option CurrentOption { get; set; }
		public object LastOption { get; set; }
		public int FirstPlayerId { get; set; }
	    public int CurrentPlayerId { get; set; }
		public ChosenEntities CurrentChosenEntites { get; set; }

		private PlayerEntity _localPlayer;
		public PlayerEntity LocalPlayer {
			get { return _localPlayer; }
			set
			{
				this._localPlayer = value;
				//Console.WriteLine("Assigned LocalPlayer: " + LocalPlayer + ", " + EventHandler);
				EventHandler?.Invoke(new GameEvent
				{
					Type = "LOCAL_PLAYER",
					Value = this._localPlayer
				});
			}
		}

		private PlayerEntity _opponentPlayer;
		public PlayerEntity OpponentPlayer
		{
			get { return _opponentPlayer; }
			set
			{
				this._opponentPlayer = value;
				//Console.WriteLine("Assigned OpponentPlayer: " + OpponentPlayer);
				EventHandler?.Invoke(new GameEvent
				{
					Type = "OPPONENT_PLAYER",
					Value = this._opponentPlayer
				});
			}
		}

		public void Reset()
		{
			Replay = new HearthstoneReplay();
			CurrentGame = new Game();
		}

		public void UpdateCurrentNode(params System.Type[] types)
		{
			while(Node.Parent != null && types.All(x => x != Node.Type))
				Node = Node.Parent;
		}

		public List<PlayerEntity> getPlayers()
		{
			List<PlayerEntity> players = new List<PlayerEntity>();
			foreach (GameData x in CurrentGame.Data)
			{
				if (x is PlayerEntity) players.Add((PlayerEntity)x);
			}
			return players;
		}

		public void TryAssignLocalPlayer()
		{
			// Only assign the local player once
			if (LocalPlayer != null && OpponentPlayer != null)
			{
				return;
			}

			// Names are not assigned right away, so wait until all the data is present to notify
			foreach (PlayerEntity player in getPlayers())
			{
				if (player.Name == null)
				{
					//Console.WriteLine("Player with no name: " + player);
					return;
				}
			}

			//Console.WriteLine("Trying to assign local player");
			List<ShowEntity> showEntities = CurrentGame.FilterGameData(typeof(ShowEntity)).Select(data => (ShowEntity)data).ToList();
			foreach (ShowEntity entity in showEntities)
			{
				//Console.WriteLine("Considering entity: " + entity);
				if (entity.CardId != null && entity.CardId.Length > 0 && GetTag(entity.Tags, GameTag.CARDTYPE) != (int)CardType.ENCHANTMENT)
				{
					int entityId = entity.Entity;
					BaseEntity fullEntity = GetEntity(entityId);
					int controllerId = GetTag(fullEntity.Tags, GameTag.CONTROLLER);
					//Console.WriteLine("Passed first step: " + entityId + ", " + fullEntity + ", " + controllerId);
					foreach (PlayerEntity player in getPlayers())
					{
						if (GetTag(player.Tags, GameTag.CONTROLLER) == controllerId)
						{
							LocalPlayer = player;
						}
					}
					if (LocalPlayer != null)
					{
						foreach (PlayerEntity player in getPlayers())
						{
							if (player == LocalPlayer)
							{
								continue;
							}
							OpponentPlayer = player;
						}
						return;
					}
				}
			}
		}

		private int GetTag(List<Tag> tags, GameTag tag)
		{
			Tag ret = tags.Where(t => t.Name == (int)tag).First();
			return ret == null ? -1 : ret.Value;
		}

		private BaseEntity GetEntity(int id)
		{
			return CurrentGame.FilterGameData(typeof(FullEntity), typeof(PlayerEntity))
				.Select(data => (BaseEntity)data).ToList()
				.Where(e => e.Id == id)
				.First();
		}
	}
}