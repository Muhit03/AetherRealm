using UnityEngine;

namespace AetherRealm
{
    public enum WeaponType { None, Sword, Staff, Bow, Club }

    // Builds a simple blocky character (head, body, two arms, two legs, a weapon)
    // out of cubes and spheres and parents it under the character object. The
    // project has no 3D models, so every character is made this way.
    public static class CharacterBuilder
    {
        public static CharacterRig Build(Transform parent, Color clothColor, Color skinColor, float size, WeaponType weapon)
        {
            CharacterRig rig = new CharacterRig();

            GameObject model = new GameObject("Model");
            model.transform.SetParent(parent, false);
            // shift down a little so the feet sit on the ground
            model.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            rig.model = model.transform;

            // Body holds the torso and head. It bobs and leans while walking.
            GameObject body = new GameObject("Body");
            body.transform.SetParent(model.transform, false);
            body.transform.localPosition = new Vector3(0f, -0.15f * size, 0f);
            rig.body = body.transform;

            AddPart(rig, "Torso", body.transform,
                new Vector3(0f, 0.45f * size, 0f),
                new Vector3(0.6f * size, 0.85f * size, 0.4f * size),
                PrimitiveType.Cube, clothColor);

            rig.head = AddPart(rig, "Head", body.transform,
                new Vector3(0f, 1.0f * size, 0f),
                Vector3.one * 0.45f,
                PrimitiveType.Sphere, skinColor);

            AddEyes(rig, rig.head);

            // Arms hang from the shoulders. The pivot is at the shoulder so the
            // arm rotates like a real arm when it swings.
            rig.rightArm = AddLimb(rig, "RightArm", body.transform, new Vector3(0.4f * size, 0.7f * size, 0f), clothColor);
            rig.leftArm = AddLimb(rig, "LeftArm", body.transform, new Vector3(-0.4f * size, 0.7f * size, 0f), clothColor);

            // Legs hang from the model (not the body) so they don't bob.
            rig.rightLeg = AddLimb(rig, "RightLeg", model.transform, new Vector3(0.16f * size, -0.2f, 0f), clothColor);
            rig.leftLeg = AddLimb(rig, "LeftLeg", model.transform, new Vector3(-0.16f * size, -0.2f, 0f), clothColor);

            AddWeapon(rig, rig.rightArm, weapon);

            return rig;
        }

        static Transform AddLimb(CharacterRig rig, string name, Transform parent, Vector3 shoulderPosition, Color color)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = shoulderPosition;

            AddPart(rig, name + "Mesh", pivot.transform,
                new Vector3(0f, -0.3f, 0f),
                new Vector3(0.16f, 0.6f, 0.16f),
                PrimitiveType.Cube, color);

            return pivot.transform;
        }

        static Transform AddPart(CharacterRig rig, string name, Transform parent, Vector3 localPosition, Vector3 scale, PrimitiveType shape, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = name;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Palette.Material(color);

            rig.renderers.Add(part.GetComponent<Renderer>());
            return part.transform;
        }

        static void AddEyes(CharacterRig rig, Transform head)
        {
            Material eyeMaterial = Palette.GlowMaterial(new Color(1f, 0.95f, 0.7f));

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                Object.Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(head, false);
                eye.transform.localPosition = new Vector3(0.15f * side, 0.05f, 0.42f);
                eye.transform.localScale = Vector3.one * 0.12f;
                eye.GetComponent<Renderer>().sharedMaterial = eyeMaterial;
                rig.renderers.Add(eye.GetComponent<Renderer>());
            }
        }

        static void AddWeapon(CharacterRig rig, Transform hand, WeaponType weapon)
        {
            Transform grip = new GameObject("Weapon").transform;
            grip.SetParent(hand, false);
            grip.localPosition = new Vector3(0f, -0.55f, 0.1f);

            if (weapon == WeaponType.Sword)
            {
                AddPart(rig, "Blade", grip, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.05f), PrimitiveType.Cube, Palette.WarriorSteel);
                AddPart(rig, "Handle", grip, new Vector3(0f, -0.1f, 0f), new Vector3(0.08f, 0.3f, 0.08f), PrimitiveType.Cube, Palette.Wood);
            }
            else if (weapon == WeaponType.Staff)
            {
                AddPart(rig, "Shaft", grip, new Vector3(0f, 0.4f, 0f), new Vector3(0.08f, 1.7f, 0.08f), PrimitiveType.Cube, Palette.Wood);
                Transform orb = AddPart(rig, "Orb", grip, new Vector3(0f, 1.3f, 0f), Vector3.one * 0.3f, PrimitiveType.Sphere, Palette.MageGlow);
                orb.GetComponent<Renderer>().sharedMaterial = Palette.GlowMaterial(Palette.MageGlow);
            }
            else if (weapon == WeaponType.Bow)
            {
                Transform bow = AddPart(rig, "Bow", grip, new Vector3(0f, 0.2f, 0f), new Vector3(0.08f, 1.4f, 0.12f), PrimitiveType.Cube, Palette.Wood);
                bow.localRotation = Quaternion.Euler(0f, 0f, 12f);
            }
            else if (weapon == WeaponType.Club)
            {
                AddPart(rig, "ClubHandle", grip, new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.8f, 0.12f), PrimitiveType.Cube, Palette.Wood);
                AddPart(rig, "ClubHead", grip, new Vector3(0f, 0.8f, 0f), new Vector3(0.35f, 0.4f, 0.35f), PrimitiveType.Cube, Palette.StoneDark);
            }
        }
    }
}
