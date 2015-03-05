using System;
using System.Drawing;
using SimpleScene.Util;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{
    /// <summary>
    /// Emits particles on demand via EmitParticles(...) or periodically via Simulate(...)
    /// </summary>
    public abstract class SSParticleEmitter
    {
        public delegate void ReceiverHandler(SSParticle newParticle);

        protected readonly static SSParticle c_defaultParticle = new SSParticle();

        protected static Random s_rand = new Random ();

        public float EmissionDelay = 0f;

        public float EmissionIntervalMin = 1.0f;
        public float EmissionIntervalMax = 1.0f;
        public float EmissionInterval {
            set { EmissionIntervalMin = EmissionIntervalMax = value; }
        }

        public int ParticlesPerEmissionMin = 1;
        public int ParticlesPerEmissionMax = 1;
        public int ParticlesPerEmission {
            set { ParticlesPerEmissionMin = ParticlesPerEmissionMax = value; }
        }

		/// <summary>
		/// When >= 0 indices emissions left. When -1 means emit infinitely (default)
		/// </summary>
		public int TotalEmissionsLeft = -1;

        public float LifeMin = c_defaultParticle.Life;
        public float LifeMax = c_defaultParticle.Life;
        public float Life {
            set { LifeMin = LifeMax = value; }
        }

        public Vector3 VelocityComponentMin = c_defaultParticle.Vel;
        public Vector3 VelocityComponentMax = c_defaultParticle.Vel;
        public Vector3 Velocity {
            set { VelocityComponentMin = VelocityComponentMax = value; }
        }

        public Vector3 OrientationMin = c_defaultParticle.Orientation;
        public Vector3 OrientationMax = c_defaultParticle.Orientation;
        public Vector3 Orientation {
            set { OrientationMin = OrientationMax = value; }
        }

        public Vector3 AngularVelocityMin = c_defaultParticle.AngularVelocity;
        public Vector3 AngularVelocityMax = c_defaultParticle.AngularVelocity;
        public Vector3 AngularVelocity {
            set { AngularVelocityMin = AngularVelocityMax = value; }
        }

        public float MasterScaleMin = c_defaultParticle.MasterScale;
        public float MasterScaleMax = c_defaultParticle.MasterScale;
        public float MasterScale {
            set { MasterScaleMin = MasterScaleMax = value; }
        }

        public Vector3 ComponentScaleMin = c_defaultParticle.ComponentScale;
        public Vector3 ComponentScaleMax = c_defaultParticle.ComponentScale;
        public Vector3 ComponentScale {
            set { ComponentScaleMin = ComponentScaleMax = value; }
        }

        public Color4 ColorComponentMin = c_defaultParticle.Color;
        public Color4 ColorComponentMax = c_defaultParticle.Color;
        public Color4 Color {
            set { ColorComponentMin = ColorComponentMax = value; }
        }

		public float MassMin = c_defaultParticle.Mass;
		public float MassMax = c_defaultParticle.Mass;
		public float Mass {
			set { MassMin = MassMax = value; }
		}

		public float RotationalInnertiaMin = c_defaultParticle.RotationalInnertia;
		public float RotationalInnertiaMax = c_defaultParticle.RotationalInnertia;
		public float RotationalInnertia {
			set { RotationalInnertiaMin = RotationalInnertiaMax = value; }
		}

		public float DragMin = c_defaultParticle.Drag;
		public float DragMax = c_defaultParticle.Drag;
		public float Drag {
			set { DragMin = DragMax = value; }
		}

		public float RotationalDragMin = c_defaultParticle.RotationalDrag;
		public float RotationalDragMax = c_defaultParticle.RotationalDrag;
		public float RotationalDrag {
			set { RotationalDragMin = RotationalDragMax = value; }
		}

        public RectangleF[] SpriteRectangles = { c_defaultParticle.SpriteRect };
        public byte[] SpriteIndices = { c_defaultParticle.SpriteIndex };

		public ushort[] EffectorMasks = { c_defaultParticle.EffectorMask };
		public ushort EffectorMask {
			set { 
				EffectorMasks = new ushort[1]; 
				EffectorMasks [0] = value;
			}
		}

        private float m_initialDelay;
        private float m_timeSinceLastEmission;
        private float m_nextEmission;

        public virtual void Reset()
        {
            m_initialDelay = EmissionDelay;
            m_timeSinceLastEmission = float.PositiveInfinity;
            m_nextEmission = 0f;
        }

        public void EmitParticles(ReceiverHandler receiver)
        {
            int numToEmit = s_rand.Next(ParticlesPerEmissionMin, ParticlesPerEmissionMax);
            emitParticles(numToEmit, receiver);
        }

        public void Simulate(float deltaT, ReceiverHandler receiver) 
        {
			if (TotalEmissionsLeft == 0) return;

            if (m_initialDelay > 0f) {
                // if initial delay is needed
                m_initialDelay -= deltaT;
                if (m_initialDelay > 0f) {
                    return;
                }
            }

            m_timeSinceLastEmission += deltaT;
            if (m_timeSinceLastEmission > m_nextEmission) {
				if (TotalEmissionsLeft > 0 && --TotalEmissionsLeft == 0) {
					Reset ();
				} else {
					m_timeSinceLastEmission = 0f;
					m_nextEmission = Interpolate.Lerp(EmissionIntervalMin, EmissionIntervalMax, 
						(float)s_rand.NextDouble());
				}
                EmitParticles(receiver);
            }
        }

        /// <summary>
        /// Convenience function.
        /// </summary>
        static protected float nextFloat()
        {
            return (float)s_rand.NextDouble();
        }

        /// <summary>
        /// Override by the derived classes to describe how new particles are emitted
        /// </summary>
        /// <param name="particleCount">Particle count.</param>
        /// <param name="receiver">Receiver.</param>
		protected virtual void emitParticles (int particleCount, ReceiverHandler receiver)
		{
			SSParticle newParticle = new SSParticle();
			for (int i = 0; i < particleCount; ++i) {
				configureNewParticle (newParticle);
				receiver (newParticle);
			}
		}

        /// <summary>
        /// To be used by derived classes for shared particle setup
        /// </summary>
        /// <param name="p">particle to setup</param>
        protected virtual void configureNewParticle(SSParticle p)
        {
            p.Life = Interpolate.Lerp(LifeMin, LifeMax, nextFloat());

            p.ComponentScale.X = Interpolate.Lerp(ComponentScaleMin.X, ComponentScaleMax.X, nextFloat());
            p.ComponentScale.Y = Interpolate.Lerp(ComponentScaleMin.Y, ComponentScaleMax.Y, nextFloat());
            p.ComponentScale.Z = Interpolate.Lerp(ComponentScaleMin.Z, ComponentScaleMax.Z, nextFloat());

            p.Orientation.X = Interpolate.Lerp(OrientationMin.X, OrientationMax.X, nextFloat());
            p.Orientation.Y = Interpolate.Lerp(OrientationMin.Y, OrientationMax.Y, nextFloat());
            p.Orientation.Z = Interpolate.Lerp(OrientationMin.Z, OrientationMax.Z, nextFloat());

            p.AngularVelocity.X = Interpolate.Lerp(AngularVelocityMin.X, AngularVelocityMax.X, nextFloat());
            p.AngularVelocity.Y = Interpolate.Lerp(AngularVelocityMin.Y, AngularVelocityMax.Y, nextFloat());
            p.AngularVelocity.Z = Interpolate.Lerp(AngularVelocityMin.Z, AngularVelocityMax.Z, nextFloat());

            p.Vel.X = Interpolate.Lerp(VelocityComponentMin.X, VelocityComponentMax.X, nextFloat());
            p.Vel.Y = Interpolate.Lerp(VelocityComponentMin.Y, VelocityComponentMax.Y, nextFloat());
            p.Vel.Z = Interpolate.Lerp(VelocityComponentMin.Z, VelocityComponentMax.Z, nextFloat());

            p.MasterScale = Interpolate.Lerp(MasterScaleMin, MasterScaleMax, nextFloat());

			p.Mass = Interpolate.Lerp (MassMin, MassMax, nextFloat ());
			p.RotationalInnertia = Interpolate.Lerp (RotationalInnertiaMin, RotationalInnertiaMax, nextFloat ());
			p.Drag = Interpolate.Lerp (DragMin, DragMax, nextFloat ());
			p.RotationalDrag = Interpolate.Lerp (RotationalDragMin, RotationalDragMax, nextFloat ());

            p.Color.R = Interpolate.Lerp(ColorComponentMin.R, ColorComponentMax.R, nextFloat());
            p.Color.G = Interpolate.Lerp(ColorComponentMin.G, ColorComponentMax.G, nextFloat());
            p.Color.B = Interpolate.Lerp(ColorComponentMin.B, ColorComponentMax.B, nextFloat());
            p.Color.A = Interpolate.Lerp(ColorComponentMin.A, ColorComponentMax.A, nextFloat());

            p.SpriteIndex = SpriteIndices [s_rand.Next(0, SpriteIndices.Length - 1)];
            p.SpriteRect = SpriteRectangles [s_rand.Next(0, SpriteRectangles.Length - 1)];

			p.EffectorMask = EffectorMasks [s_rand.Next (0, EffectorMasks.Length - 1)];
        }
    }

	/// <summary>
	/// Emits particles at the origin
	/// </summary>
	public class SSFixedPositionEmitter : SSParticleEmitter
	{
		public Vector3 Position;

		public SSFixedPositionEmitter (Vector3 position)
		{
			Position = position;
		}

		public SSFixedPositionEmitter()
		{
			Position = Vector3.Zero;
		}

		protected override void configureNewParticle (SSParticle p)
		{
			base.configureNewParticle (p);
			p.Pos = Position;
		}
	}

	/// <summary>
	/// Emitter that sends things moving away from the center in different directions 
	/// </summary>
	public class SSRadialEmitter : SSParticleEmitter
	{
		public Vector3 Center = Vector3.Zero;

		#region spawn radius
		public float RMin = 0f;
		public float RMax = 0f;
		public float R {
			set { RMin = RMax = value; }
		}
		#endregion

		#region theta of the spawn and velocity
		public float ThetaMin = 0f;
		public float ThetaMax = 2f * (float)Math.PI;
		public float Theta {
			set { ThetaMin = ThetaMax = value; }
		}
		#endregion

		#region phi of the spawn and velocity
		public float PhiMin = -0.5f * (float)Math.PI;
		public float PhiMax = +0.5f * (float)Math.PI;
		public float Phi {
			set { PhiMin = PhiMax = value; }
		}
		#endregion

		#region magnitude of the spawns' velocity
		public float VelocityMagnitudeMin = 1f;
		public float VelocityMagnitudeMax = 1f;
		public float VelocityMagnitude {
			set { VelocityMagnitudeMin = VelocityMagnitudeMax = value; }
		}
		#endregion

		protected override void configureNewParticle (SSParticle p)
		{
			base.configureNewParticle (p);
			float r = Interpolate.Lerp (RMin, RMax, nextFloat());
			float theta = Interpolate.Lerp (ThetaMin, ThetaMax, nextFloat ());
			float phi = Interpolate.Lerp (PhiMin, PhiMax, nextFloat ());
			float xy = (float)Math.Cos (phi);
			float x = xy * (float)Math.Cos (theta);
			float y = xy * (float)Math.Sin (theta);
			float z = (float)Math.Sin (phi);
			Vector3 xyz = new Vector3 (x, y, z);
			p.Pos = Center + r * xyz;
			float velocityMag = Interpolate.Lerp (VelocityMagnitudeMin, VelocityMagnitudeMax, nextFloat ());
			p.Vel = velocityMag * xyz;
		}
	}

    /// <summary>
    /// Emits via an instance of ParticlesFieldGenerator
    /// </summary>
    public class SSParticlesFieldEmitter : SSParticleEmitter
    {
        protected ParticlesFieldGenerator m_fieldGenerator;

		public ParticlesFieldGenerator Field  {
			get { return m_fieldGenerator; }
		}

        public SSParticlesFieldEmitter(ParticlesFieldGenerator fieldGenerator)
        {
            m_fieldGenerator = fieldGenerator;
        }

        public void SetSeed(int seed)
        {
            m_fieldGenerator.SetSeed(seed);
        }

        protected override void emitParticles (int particleCount, ReceiverHandler particleReceiver)
        {
            SSParticle newParticle = new SSParticle();
            ParticlesFieldGenerator.NewParticleDelegate fieldReceiver = (id, pos) => {
                configureNewParticle(newParticle);
                newParticle.Pos = pos;
                particleReceiver(newParticle);
                return true;
            };
            m_fieldGenerator.Generate(particleCount, fieldReceiver);
        }
    }

	public class SSBodiesFieldEmitter : SSParticleEmitter
	{
		protected BodiesFieldGenerator m_bodiesGenerator;

		public SSBodiesFieldEmitter(BodiesFieldGenerator fieldGenerator)
		{
			m_bodiesGenerator = fieldGenerator;
		}

		public void SetSeed(int seed)
		{
			m_bodiesGenerator.SetSeed(seed);
		}

		protected override void emitParticles (int particleCount, ReceiverHandler particleReceiver)
		{
			SSParticle newParticle = new SSParticle();
			BodiesFieldGenerator.NewBodyDelegate bodyReceiver = (id, scale, pos, orient) => {
				configureNewParticle(newParticle);
				newParticle.Pos = pos;
				newParticle.MasterScale *= scale;
				newParticle.Orientation += OpenTKHelper.ToEuler(ref orient);
				particleReceiver(newParticle);
				return true;
			};
			m_bodiesGenerator.Generate(particleCount, bodyReceiver);
		}
	}
}

