using System.Collections.Generic;
using UnityEngine;

namespace AetherRealm
{
    // The set of body parts that ProceduralAnimator moves around. It is filled
    // in by CharacterBuilder when a character is created.
    public class CharacterRig
    {
        public Transform model;       // the whole body, child of the character
        public Transform body;        // torso + head, bobs while walking
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public List<Renderer> renderers = new List<Renderer>();
    }
}
