using System;
using System.Collections;
using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public static class Utils
    {
        public static IEnumerator SimulateForSeconds(float seconds, Action onFixedStep = null)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                onFixedStep?.Invoke();
            }
        }
    }
}