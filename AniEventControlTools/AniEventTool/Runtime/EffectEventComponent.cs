using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool
{
    public class EffectEventComponent : MonoBehaviour
    {
        public static float SimulateSpeed = 1.0f;
        public bool deactiveLoop = false;
        public float loopLifeTime = 0f;
        public ParticleSystem[] psArray = null;

        public void Init() 
        {
            psArray = GetComponentsInChildren<ParticleSystem>();
            loopLifeTime = 0;
            foreach (ParticleSystem ps in psArray)
            {
                if (loopLifeTime < ps.main.duration)
                    loopLifeTime = ps.main.duration;
            }
        }
        public void SetParticleLoopValue(bool value)
        {
            if (psArray == null)
                return;

            foreach (ParticleSystem ps in psArray)
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = value;
            }
        }

        public void SetSimulateSpeed(float value) 
        {
            if (psArray == null)
                return;

            foreach (ParticleSystem ps in psArray)
            {
                ParticleSystem.MainModule main = ps.main;
                main.simulationSpeed = value;
            }
        }
    }

}