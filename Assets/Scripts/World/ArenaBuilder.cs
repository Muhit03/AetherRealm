using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace AetherRealm
{
    // Holds the important spots in the arena so other scripts know where to
    // place the player and the NPC, where enemies appear, and where the cover
    // walls are (the archers hide behind these).
    public class ArenaLayout
    {
        public Vector3 playerStart = new Vector3(0f, 1f, -6f);
        public Vector3 npcPoint = new Vector3(-9f, 0f, 9f);
        public List<Vector3> spawnPoints = new List<Vector3>();
        public List<Vector3> coverPoints = new List<Vector3>();
    }

    // Builds the whole play area from primitives when the game starts, then
    // bakes a NavMesh so the enemies can path around the walls and pillars.
    public static class ArenaBuilder
    {
        const float HalfSize = 22f;

        public static ArenaLayout Build()
        {
            GameObject root = new GameObject("Arena");
            ArenaLayout layout = new ArenaLayout();

            BuildFloor(root.transform);
            BuildOuterWalls(root.transform);
            BuildPillarsAndTorches(root.transform);
            BuildCover(root.transform, layout);
            BuildSpawnPortals(root.transform, layout);

            BakeNavMesh(root);

            return layout;
        }

        static void BuildFloor(Transform parent)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(parent);
            floor.transform.localScale = new Vector3(HalfSize * 0.2f, 1f, HalfSize * 0.2f);
            floor.GetComponent<Renderer>().sharedMaterial = Palette.Material(Palette.Ground);
        }

        static void BuildOuterWalls(Transform parent)
        {
            float height = 6f;
            float thickness = 1.5f;
            float span = HalfSize * 2f + thickness;

            MakeWall(parent, "WallNorth", new Vector3(0f, height / 2f, HalfSize), new Vector3(span, height, thickness));
            MakeWall(parent, "WallSouth", new Vector3(0f, height / 2f, -HalfSize), new Vector3(span, height, thickness));
            MakeWall(parent, "WallEast", new Vector3(HalfSize, height / 2f, 0f), new Vector3(thickness, height, span));
            MakeWall(parent, "WallWest", new Vector3(-HalfSize, height / 2f, 0f), new Vector3(thickness, height, span));
        }

        static void MakeWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = size;
            wall.GetComponent<Renderer>().sharedMaterial = Palette.Material(Palette.Stone);
        }

        static void BuildPillarsAndTorches(Transform parent)
        {
            Vector3[] corners =
            {
                new Vector3(13f, 0f, 13f),
                new Vector3(-13f, 0f, 13f),
                new Vector3(13f, 0f, -13f),
                new Vector3(-13f, 0f, -13f)
            };

            foreach (Vector3 corner in corners)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "Pillar";
                pillar.transform.SetParent(parent);
                pillar.transform.position = corner + Vector3.up * 3.5f;
                pillar.transform.localScale = new Vector3(2f, 3.5f, 2f);
                pillar.GetComponent<Renderer>().sharedMaterial = Palette.Material(Palette.Stone);

                Torch.Create(corner + Vector3.up * 4.5f);
            }
        }

        static void BuildCover(Transform parent, ArenaLayout layout)
        {
            // Low walls scattered around the middle. Archers duck behind these.
            Vector3[] spots =
            {
                new Vector3(7f, 0.9f, 4f),
                new Vector3(-7f, 0.9f, -4f),
                new Vector3(5f, 0.9f, -8f),
                new Vector3(-5f, 0.9f, 8f),
                new Vector3(0f, 0.9f, 10f),
                new Vector3(0f, 0.9f, -10f)
            };

            foreach (Vector3 spot in spots)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "CoverWall";
                block.transform.SetParent(parent);
                block.transform.position = spot;
                block.transform.localScale = new Vector3(3.5f, 1.8f, 1f);
                block.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
                block.GetComponent<Renderer>().sharedMaterial = Palette.Material(Palette.StoneDark);

                layout.coverPoints.Add(spot);
            }
        }

        static void BuildSpawnPortals(Transform parent, ArenaLayout layout)
        {
            // Kept well inside the walls so the enemies land on the baked NavMesh.
            Vector3[] positions =
            {
                new Vector3(0f, 0f, 17f),
                new Vector3(0f, 0f, -17f),
                new Vector3(17f, 0f, 0f),
                new Vector3(-17f, 0f, 0f)
            };

            foreach (Vector3 position in positions)
            {
                GameObject portal = new GameObject("SpawnPortal");
                portal.transform.SetParent(parent);
                portal.transform.position = position;

                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Disc";
                Object.Destroy(disc.GetComponent<Collider>());
                disc.transform.SetParent(portal.transform);
                disc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                disc.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
                disc.GetComponent<Renderer>().sharedMaterial = Palette.GlowMaterial(Palette.Portal);

                Light light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(portal.transform);
                light.transform.localPosition = Vector3.up;
                light.color = Palette.Portal;
                light.range = 7f;
                light.intensity = 1.5f;

                layout.spawnPoints.Add(position);
            }
        }

        static void BakeNavMesh(GameObject arenaRoot)
        {
            NavMeshSurface surface = arenaRoot.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }
    }
}
