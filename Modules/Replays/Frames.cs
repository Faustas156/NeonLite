using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NeonLite.Modules.Replays
{
    internal static class Frames
    {
        public interface IFrame {};
        public struct PlayerFrame : IFrame // tick
        {
            public Vector3 motorPos;
            public Vector3 velocity;
            public Vector3 movementVel;
            public Vector3 endingVel;

            public float moveStun;

            public float inputX;
            public float inputY;
            public bool jumpDown;
            public bool jumpHeld;
            public bool jumpUp;
            public bool jumpRelease;

            public Quaternion camRotation;
        }

        public struct ProjectileFrame : IFrame // tick
        {
            public string id;
            public Vector3 position;
            public Vector3 direction;
        }

        public struct EnemyProjectileFrame : IFrame // tick
        {
            public int enemy;
            public int weaponIndex;
            public int projectileIndex;
            public Vector3 position;
            public Vector3 direction;
        }

        public struct HitscanFrame : IFrame // tick
        {
            public Ray ray;
            public float dist;
            public int damage;
            public float knockback;
            public bool canParry;
            public bool isAbility;
            public bool isSwipe;
        }

        public struct FunctionalFrame : IFrame // can be tick or timer
        {
            public MethodBase method;
            public object instance;
            public object[] args;
        }

        public struct EnemyKillFrame : IFrame // tick
        {
            public int enemy;
            public BaseDamageable.DamageSource damageSource;
        }

        public struct MouseLookFrame : IFrame // timer
        {
            public float rotationX;
            public float rotationY;
        }

        public struct SwapFrame : IFrame { }; //timer, empty
    }
}
