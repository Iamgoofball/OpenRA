#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CncShellmapScriptInfo : ITraitInfo
	{
		public string Music = "otp";
		public object Create(ActorInitializer init) { return new CncShellmapScript(this); }
	}

	class CncShellmapScript: IWorldLoaded, ITick
	{		
		CncShellmapScriptInfo Info;
		Dictionary<string, Actor> Actors;
		static int2 ViewportOrigin;

		public CncShellmapScript(CncShellmapScriptInfo info)
		{
			Info = info;
		}

		public void WorldLoaded(World w)
		{
			var b = w.Map.Bounds;
			ViewportOrigin = new int2(b.Left + b.Width/2, b.Top + b.Height/2);
			Game.MoveViewport(ViewportOrigin);

			Actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			// Mute world sounds
			Sound.SoundVolumeModifier = 0f;

			LoopMusic();
		}

		void LoopMusic()
		{
			if (!Game.Settings.Game.ShellmapMusic ||
			    	Info.Music == null ||
			    	!Rules.Music[Info.Music].Exists)
				return;

			Sound.PlayMusicThen(Rules.Music[Info.Music].Filename, () => LoopMusic());
		}
		
		int ticks = 0;
		float speed = 4f;
		public void Tick(Actor self)
		{
			var loc = new float2(
				(float)(-System.Math.Sin((ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed) * 15f + ViewportOrigin.X),
				(float)(0.4f*System.Math.Cos((ticks + 45) % (360f * speed) * (Math.PI / 180) * 1f / speed) * 10f + ViewportOrigin.Y));
			Game.MoveViewport(loc);
			
			if (ticks == 0)
			{
				LoopTrack(Actors["boat1"], Actors["tl1"].Location, Actors["tr1"].Location);
				LoopTrack(Actors["boat3"], Actors["tl1"].Location, Actors["tr1"].Location);
				LoopTrack(Actors["boat2"], Actors["tl3"].Location, Actors["tr3"].Location);
				LoopTrack(Actors["boat4"], Actors["tl3"].Location, Actors["tr3"].Location);
				CreateUnitsInTransport(Actors["lst1"], new string[] {"htnk"});
				CreateUnitsInTransport(Actors["lst2"], new string[] {"mcv"});
				CreateUnitsInTransport(Actors["lst3"], new string[] {"htnk"});
				LoopTrack(Actors["lst1"], Actors["tl2"].Location, Actors["tr2"].Location);
				LoopTrack(Actors["lst2"], Actors["tl2"].Location, Actors["tr2"].Location);
				LoopTrack(Actors["lst3"], Actors["tl2"].Location, Actors["tr2"].Location);
			}
			
			ticks++;
		}
		
		void CreateUnitsInTransport(Actor transport, string[] cargo)
		{
			var f = transport.Trait<IFacing>();
			var c = transport.Trait<Cargo>();
			foreach (var i in cargo)
				c.Load(transport, transport.World.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary
				{
					new OwnerInit( transport.Owner ),
					new FacingInit( f.Facing ),
				}));
		}
		
		void LoopTrack(Actor self, int2 left, int2 right)
		{
			var mobile = self.Trait<Mobile>();
			self.QueueActivity(mobile.ScriptedMove(left));
			self.QueueActivity(new Teleport(right));
			self.QueueActivity(new CallFunc(() => LoopTrack(self,left,right)));
		}
	}
}
