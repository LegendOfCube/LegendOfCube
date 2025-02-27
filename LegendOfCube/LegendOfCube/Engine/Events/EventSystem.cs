﻿using System;
using System.Linq;
using LegendOfCube.Engine.Events;
using Microsoft.Xna.Framework;
using LegendOfCube.Engine.BoundingVolumes;

namespace LegendOfCube.Engine
{
	class EventSystem
	{
		private static readonly float GROUND_WALL_ANGLE = MathHelper.ToRadians(75.0f); // 0 < angle < 90
		private static readonly float ON_WALL_LIMIT = (float)Math.Sin(GROUND_WALL_ANGLE);
		private static readonly float ON_GROUND_LIMIT = (float)Math.Cos(GROUND_WALL_ANGLE);

		private const float WALL_QUERY_EPSILON = 0.035f;

		private static PlayerCubeState lastCubeState;

		public static void CalculateCubeState(World world, PhysicsSystem physicsSystem)
		{
			// Defaults to not being on the ground or on the wall
			world.PlayerCubeState.OnGround = false;
			world.PlayerCubeState.OnWall = false;
			world.PlayerCubeState.GroundAxis = Vector3.Zero;
			world.PlayerCubeState.WallAxis = Vector3.Zero;

			// Check if player cube collided with any walls or grounds
			foreach (var e in world.EventBuffer.CollisionEvents)
			{
				float xDot = Math.Abs(e.Axis.X);
				float zDot = Math.Abs(e.Axis.Z);
				float wallDot = (float)Math.Sqrt(xDot * xDot + zDot * zDot);
				if (e.Collider.Id == world.Player.Id || e.CollidedWith.Id == world.Player.Id)
				{
					if (e.Axis.Y > ON_GROUND_LIMIT)
					{
						world.PlayerCubeState.OnGround = true;
						world.PlayerCubeState.GroundAxis = (e.Collider.Id == world.Player.Id ? 1.0f : -1.0f) * e.Axis;
					}
					else if (wallDot > ON_WALL_LIMIT)
					{
						world.PlayerCubeState.OnWall = true;
						world.PlayerCubeState.WallAxis = (e.Collider.Id == world.Player.Id ? 1.0f : -1.0f) * e.Axis;
					}
				}
			}

			// If Cube is on the ground it can't be on a wall.
			if (world.PlayerCubeState.OnGround)
			{
				world.PlayerCubeState.OnWall = false;
				world.PlayerCubeState.WallAxis = Vector3.Zero;
			}

			// Wall queries
			if (lastCubeState.OnGround && !world.PlayerCubeState.OnGround)
			{
				OBB wsCubeOBB = physicsSystem.WorldSpaceOBBs[world.Player.Id];
				wsCubeOBB.Position = wsCubeOBB.Position - lastCubeState.GroundAxis * WALL_QUERY_EPSILON;
				UInt32 colId = FindIntersection(world, world.Player.Id, ref wsCubeOBB, physicsSystem);
				if (colId != UInt32.MaxValue)
				{
					world.PlayerCubeState.OnGround = true;
					world.PlayerCubeState.GroundAxis = lastCubeState.GroundAxis; // TODO: Disgusting, ugly hack. Fix plx.
				}
			}
			else if (lastCubeState.OnWall && !world.PlayerCubeState.OnGround && !world.PlayerCubeState.OnWall)
			{
				OBB wsCubeOBB = physicsSystem.WorldSpaceOBBs[world.Player.Id];
				wsCubeOBB.Position = wsCubeOBB.Position - lastCubeState.WallAxis * WALL_QUERY_EPSILON;
				UInt32 colId = FindIntersection(world, world.Player.Id, ref wsCubeOBB, physicsSystem);
				if (colId != UInt32.MaxValue)
				{
					world.PlayerCubeState.OnWall = true;
					world.PlayerCubeState.WallAxis = lastCubeState.WallAxis; // TODO: Disgusting, ugly hack. Fix plx.
				}
			}

			lastCubeState = world.PlayerCubeState;
		}

		public static void HandleEvents(World world)
		{

			EventBuffer eventBuffer = world.EventBuffer;

			foreach (var collisionEvent in eventBuffer.CollisionEvents)
			{
				var collidedWith = collisionEvent.CollidedWith.Id;
				var collider = collisionEvent.Collider.Id;

				if (world.EntityProperties[collidedWith].Satisfies(Properties.CHECKPOINT_FLAG))
				{
					if (collider == world.Player.Id)
					{
						world.SpawnPoint = world.Transforms[collidedWith].Translation;
					}
				}
				else if (world.EntityProperties[collider].Satisfies(Properties.CHECKPOINT_FLAG))
				{
					if (collidedWith == world.Player.Id)
					{
						if (world.SpawnPoint != world.Transforms[collider].Translation)
						{
							world.SpawnPoint = world.Transforms[collider].Translation;
							world.CheckpointsPassed += 1;
						}
					}
				}

				if (world.EntityProperties[collidedWith].Satisfies(Properties.TELEPORT_FLAG))
				{
					Random rnd = new Random();
					var dest = world.EnumerateEntities(new Properties(Properties.TELEPORT_FLAG)).Where(entity => entity.Id != collidedWith).ToList();
					int teleportTo = (int)dest[rnd.Next(dest.Count)].Id;
					world.Transforms[collider].Translation = world.Transforms[teleportTo].Translation - 5 * collisionEvent.Axis;
					world.Velocities[collider] = collisionEvent.ColliderVelocity;
				}
				if (world.EntityProperties[collidedWith].Satisfies(Properties.BOUNCE_FLAG))
				{
					world.Velocities[collider] = Vector3.Reflect(collisionEvent.ColliderVelocity, collisionEvent.Axis);
					world.PlayerCubeState.OnGround = false;
					world.PlayerCubeState.OnWall = false;
				}
			}

			if (eventBuffer.CollisionEvents.Any(c => EventUtils.PlayerShouldWin(world, c)))
			{
				world.WinState = true;
				return;
			}

			if (eventBuffer.CollisionEvents.Any(c => EventUtils.PlayerShouldDie(world, c)))
			{
				RespawnPlayer(world);
			}

		}

		public static void RespawnPlayer(World world)
		{
			world.Transforms[world.Player.Id].Translation = world.SpawnPoint;
			world.Velocities[world.Player.Id] = Vector3.Zero;
			world.WinState = false;
			world.TimeSinceGameOver = 0;
			world.GameStats.PlayerDeaths += 1;
			world.GameStats.GameTime = 0;
			world.PlayerRespawAudioCue = true;
		}

		// Precondition: param entity must satisfy MOVABLE
		// Returns UInt32.MaxValue if no intersections are found, otherwise index of entity collided with.
		private static UInt32 FindIntersection(World world, UInt32 entity, ref OBB entityOBB, PhysicsSystem physicsSystem)
		{
			for (UInt32 i = 0; i <= world.HighestOccupiedId; i++)
			{
				if (!world.EntityProperties[i].Satisfies(Properties.MODEL_SPACE_BV | Properties.TRANSFORM)) continue;
				if (i == entity) continue;
				if (physicsSystem.WorldSpaceOBBs[i].Intersects(ref entityOBB)) return i;
			}

			// No collisions found.
			return UInt32.MaxValue;
		}
	}
}
