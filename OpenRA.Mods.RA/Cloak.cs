﻿#region Copyright & License Information
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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CloakInfo : ITraitInfo
	{
		public int InitialDelay = 10; // Ticks
		public int CloakDelay = 30; // Ticks
		public string CloakSound = "subshow1.aud";
		public string UncloakSound = "subshow1.aud";
		public readonly string Palette = "cloak";
		public readonly bool UncloakOnMove = false;

		public object Create(ActorInitializer init) { return new Cloak(init.self, this); }
	}

	public class Cloak : IRenderModifier, INotifyDamageStateChanged, INotifyAttack, ITick, IVisibilityModifier, IRadarColorModifier, ISync
	{
		[Sync] int remainingTime;
		[Sync] bool canCloak = true;

		Actor self;
		CloakInfo info;
		CPos? lastPos;

		public Cloak(Actor self, CloakInfo info)
		{
			this.info = info;
			this.self = self;

			remainingTime = info.InitialDelay;
		}

		public void Uncloak() { Uncloak(info.CloakDelay); }

		public void Uncloak(int time)
		{
			if (Cloaked)
				Sound.Play(info.UncloakSound, self.CenterLocation);

			remainingTime = Math.Max(remainingTime, time);
		}

		public void Attacking(Actor self, Target target) { Uncloak(); }

		public bool Cloaked { get { return remainingTime <= 0; } }

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			canCloak = (e.DamageState < DamageState.Critical);
			if (!canCloak) Uncloak();
		}

		static readonly Renderable[] Nothing = { };

		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			if (remainingTime > 0)
				return r;

			if (Cloaked && IsVisible(self.World.RenderedShroud, self))
				return r.Select(a => a.WithPalette(wr.Palette(info.Palette)));
			else
				return Nothing;
		}

		public void Tick(Actor self)
		{
			if (remainingTime > 0 && canCloak)
				if (--remainingTime <= 0)
					Sound.Play(info.CloakSound, self.CenterLocation);
			if (self.IsDisabled())
				Uncloak();

			if (info.UncloakOnMove && (lastPos == null || lastPos.Value != self.Location))
			{
				Uncloak();
				lastPos = self.Location;
			}
		}
		
		public bool IsVisible(Shroud s, Actor self)
		{
			if (!Cloaked)
				return true;

			if (s != null)
			{
				if (s == self.World.LocalShroud && s.Observing)
					return true;
				if (s.Owner != null)
					if (self.Owner == s.Owner || self.Owner.Stances[s.Owner] == Stance.Ally)
						return true;
			}

			if (self.World.LocalPlayer != null)
				if (self.Owner == self.World.LocalPlayer || self.Owner.Stances[self.World.LocalPlayer] == Stance.Ally)
					return true;
			
			return self.World.ActorsWithTrait<DetectCloaked>().Any(a =>
				a.Actor.Owner.Stances[self.Owner] != Stance.Ally &&
				(self.Location - a.Actor.Location).Length < a.Actor.Info.Traits.Get<DetectCloakedInfo>().Range);
		}

		public Color RadarColorOverride(Actor self)
		{
			var c = self.Owner.ColorRamp.GetColor(0);
			if (self.Owner == self.World.LocalPlayer && Cloaked)
				c = Color.FromArgb(128, c);
			return c;
		}
	}
}
