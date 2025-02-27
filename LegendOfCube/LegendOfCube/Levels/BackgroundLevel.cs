﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LegendOfCube.Engine;
using LegendOfCube.Engine.Graphics;
using Microsoft.Xna.Framework;

namespace LegendOfCube.Levels
{
	class BackgroundLevel : Level
	{
		public BackgroundLevel() : base("BackgroundLevel", true) {}

		public override World CreateWorld(Game game, ContentCollection contentCollection)
		{
			World world = new World(1000)
			{
				SpawnPoint = new Vector3(0, 8, 0),
				DirLight = new DirLight(Vector3.Normalize(new Vector3(3.5f, -3.0f, -3.0f))),
				InitialViewDirection = Vector3.Normalize(new Vector3(1, 0, 0)),
			};

			world.Camera = new Camera(
				new Vector3(-10, 0, 50),
				new Vector3(-10, -12, 0),
				75.0f);

			var playerBuilder = new EntityBuilder()
				.WithModelData(contentCollection.PlayerCube2)
				.WithTransform(Matrix.CreateScale(2.5f))
				.WithPosition(world.SpawnPoint)
				.WithVelocity(Vector3.Zero, 15)
				.WithAcceleration(Vector3.Zero)
				.WithAdditionalProperties(
					new Properties(Properties.INPUT | Properties.GRAVITY_FLAG | Properties.DYNAMIC_VELOCITY_FLAG))
				.AddToWorld(world);

			var groundWoodBuilder = new EntityBuilder().WithModelData(contentCollection.GroundWood);
			var ductBuilder = new EntityBuilder().WithModelData(contentCollection.Duct);
			var placeholderWallBuilder = new EntityBuilder().WithModelData(contentCollection.placeholderWall);

			placeholderWallBuilder.Copy()
				.WithTransform(Matrix.CreateScale(10) * Matrix.CreateRotationX(MathHelper.ToRadians(90)))
				.WithPosition(0, 0, -3).AddToWorld(world);

			ductBuilder.Copy().WithPosition(0, 10, 0).AddToWorld(world);
			ductBuilder.Copy().WithPosition(0, 20, 0).AddToWorld(world);
			ductBuilder.Copy().WithPosition(0, 30, 0).AddToWorld(world);
			ductBuilder.Copy().WithPosition(0, 40, 0).AddToWorld(world);

			groundWoodBuilder.Copy().WithTransform(Matrix.CreateScale(0.14f, 0.1f, 1f)* Matrix.CreateRotationX(MathHelper.ToRadians(-25))
				 * Matrix.CreateRotationY(MathHelper.ToRadians(90))).WithPosition(0, 0, 0).AddToWorld(world);

			groundWoodBuilder.Copy().WithTransform(Matrix.CreateScale(0.14f, 0.1f, 1f) * Matrix.CreateRotationX(MathHelper.ToRadians(25))
				 * Matrix.CreateRotationY(MathHelper.ToRadians(90))).WithPosition(-20, -20, 0).AddToWorld(world);

			groundWoodBuilder.Copy().WithTransform(Matrix.CreateScale(0.14f, 0.1f, 1f) * Matrix.CreateRotationX(MathHelper.ToRadians(-25))
				 * Matrix.CreateRotationY(MathHelper.ToRadians(90))).WithPosition(5, -40, 0).AddToWorld(world);


			ductBuilder.Copy().WithPosition(-25, -65, 0).WithAdditionalProperties(new Properties(Properties.DEATH_ZONE_FLAG)).AddToWorld(world);
			ductBuilder.Copy().WithPosition(-25, -75, 0).AddToWorld(world);
			ductBuilder.Copy().WithPosition(-25, -85, 0).AddToWorld(world);
			ductBuilder.Copy().WithPosition(-25, -95, 0).AddToWorld(world);

			return world;
		}
	}
}
