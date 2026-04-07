using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// This decision will return true when the target path reaches its end (this requires it to be in OnlyOnce cycle mode) 
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision MM Path End Reached")]
    public class AIDecisionMMPathEndReached : AIDecision
    {
        public MMPath TargetPath;
        
        /// <summary>
        /// We return true on Decide
        /// </summary>
        /// <returns></returns>
        public override bool Decide()
        {
            return TargetPath.EndReached;
        }
    }
}
