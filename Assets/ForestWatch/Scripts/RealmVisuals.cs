using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForestWatch
{
    /// <summary>Original, procedural low-poly art for the forest defense prototype.</summary>
    public static class RealmVisuals
    {
        public static Vector3[] Route { get; private set; } = DefenseLevel.DefaultRoute();
        public static Vector3[] BuildSites { get; private set; } = DefenseLevel.DefaultBuildSites();
        public static Vector3 HeroStart { get; private set; } = new Vector3(-5, .2f, -2);
        public static Vector3 CampPosition { get; private set; } = new Vector3(-3, .2f, 7);
        public static DefenseLevel ActiveLevel { get; private set; }

        public static void ApplyLevel(DefenseLevel level)
        {
            ActiveLevel = level != null && level.IsPlayable ? level : null;
            Route = Grounded(ActiveLevel != null ? ActiveLevel.route : DefenseLevel.DefaultRoute());
            BuildSites = Grounded(ActiveLevel != null ? ActiveLevel.buildSites : DefenseLevel.DefaultBuildSites());
            HeroStart = Ground(ActiveLevel != null ? ActiveLevel.heroStart : new Vector3(-5, .2f, -2));
            CampPosition = Ground(ActiveLevel != null ? ActiveLevel.campPosition : new Vector3(-3, .2f, 7));
        }

        static Vector3 Ground(Vector3 position) { position.y = .2f; return position; }
        static Vector3[] Grounded(Vector3[] positions)
        {
            var copy = new Vector3[positions.Length];
            for (int i = 0; i < positions.Length; i++) copy[i] = Ground(positions[i]);
            return copy;
        }

        static Color Grass => ActiveLevel != null ? ActiveLevel.grassColor : Hex(0x5B813E);
        static Color FoliageDark => ActiveLevel != null ? ActiveLevel.foliageDark : Hex(0x2E6E4A);
        static Color FoliageMid => ActiveLevel != null ? ActiveLevel.foliageMid : Hex(0x3F8952);
        static Color FoliageLight => ActiveLevel != null ? ActiveLevel.foliageLight : Hex(0x6B9C58);

        static readonly Dictionary<int, Material> Materials = new Dictionary<int, Material>();
        static readonly Dictionary<int, Mesh> FrustumMeshes = new Dictionary<int, Mesh>();
        static readonly Color Stone = Hex(0x8C9998);
        static readonly Color StoneLight = Hex(0xB8C0AD);
        static readonly Color StoneDark = Hex(0x536568);
        static readonly Color Wood = Hex(0x76503B);
        static readonly Color WoodLight = Hex(0xAE7943);
        static readonly Color Gold = Hex(0xF2C46C);
        static readonly Color Teal = Hex(0x5DE5CB);
        static readonly Color Blue = Hex(0x66BCE6);

        static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        }

        public static Material Material(Color color, bool emissive = false)
        {
            Color32 c = color;
            int key = c.r | (c.g << 8) | (c.b << 16) | (emissive ? 1 << 24 : 0);
            if (Materials.TryGetValue(key, out Material existing) && existing != null) return existing;
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new System.InvalidOperationException("The prototype requires the Built-in Standard shader.");
            Material material = new Material(shader) { color = color, name = "Forest " + key };
            material.SetFloat("_Glossiness", 0.08f);
            material.SetFloat("_Metallic", 0);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.75f);
            }
            Materials[key] = material;
            return material;
        }

        static GameObject Root(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            }
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 p, Vector3 s, Color color, float rotation = 0)
        {
            GameObject go = Primitive(PrimitiveType.Cube, name, parent, p, s, color);
            go.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            return go;
        }

        static GameObject Frustum(string name, Transform parent, Vector3 p, float radiusBottom, float radiusTop, float height, Color color, int sides = 8)
        {
            int key = sides * 100000 + Mathf.RoundToInt(radiusTop / Mathf.Max(radiusBottom, .001f) * 1000);
            if (!FrustumMeshes.TryGetValue(key, out Mesh mesh) || mesh == null)
            {
                float ratio = radiusTop / Mathf.Max(radiusBottom, .001f);
                List<Vector3> vertices = new List<Vector3>();
                List<int> indices = new List<int>();
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides;
                    float b = (i + 1) * Mathf.PI * 2 / sides;
                    Vector3 p0 = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    Vector3 p1 = new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b));
                    int start = vertices.Count;
                    vertices.Add(p0); vertices.Add(p1);
                    vertices.Add(new Vector3(p0.x * ratio, 1, p0.z * ratio));
                    vertices.Add(new Vector3(p1.x * ratio, 1, p1.z * ratio));
                    indices.AddRange(new[] { start, start + 1, start + 2, start + 1, start + 3, start + 2 });
                    start = vertices.Count;
                    vertices.Add(Vector3.zero); vertices.Add(p1); vertices.Add(p0);
                    indices.AddRange(new[] { start, start + 1, start + 2 });
                    start = vertices.Count;
                    vertices.Add(Vector3.up);
                    vertices.Add(new Vector3(p0.x * ratio, 1, p0.z * ratio));
                    vertices.Add(new Vector3(p1.x * ratio, 1, p1.z * ratio));
                    indices.AddRange(new[] { start, start + 1, start + 2 });
                }
                mesh = new Mesh { name = "Low Poly " + sides + " Sides" };
                mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                FrustumMeshes[key] = mesh;
            }
            GameObject go = Root(name, parent, p);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
            go.transform.localScale = new Vector3(radiusBottom, height, radiusBottom);
            return go;
        }

        static GameObject Facet(string name, Transform parent, Vector3 p, Vector3 size, Color color)
        {
            GameObject go = Primitive(PrimitiveType.Sphere, name, parent, p, size, color);
            return go;
        }

        public static GameObject Orb(Transform parent, Vector3 pos, float radius, Color color)
        {
            GameObject orb = Primitive(PrimitiveType.Sphere, "Magic Orb", parent, pos, Vector3.one * radius * 2, color);
            orb.GetComponent<Renderer>().sharedMaterial = Material(color, true);
            return orb;
        }

        public static GameObject Ring(Transform parent, Vector3 pos, float radius, Color color, float width = 0.05f)
        {
            GameObject ring = Root("Selection Ring", parent, pos);
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            line.startWidth = width;
            line.endWidth = width;
            Shader shader = Shader.Find("Sprites/Default");
            line.sharedMaterial = shader != null ? new Material(shader) : Material(color, true);
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i * Mathf.PI * 2 / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Sin(a) * radius, 0.04f, Mathf.Cos(a) * radius));
            }
            return ring;
        }

        public static void BuildMap(Transform parent)
        {
            Random.State previousRandom = Random.state;
            Random.InitState(ActiveLevel != null ? ActiveLevel.seed : 41027);
            BuildLighting(parent);

            Transform land = Root("Verdant Crossing", parent, Vector3.zero).transform;
            IslandLayer(land, "Lower Soil", -1.6f, -0.55f, 0.93f, 1, Hex(0x584637));
            IslandLayer(land, "Earthen Edge", -0.55f, 0.02f, 1, 1.018f, Hex(0x8D7152));
            IslandLayer(land, "Grass Edge", 0.02f, 0.12f, 1.018f, 1, Grass * .8f);
            IslandTop(land, Grass);
            GameObject ground = Root("Ground", land, new Vector3(0, -0.15f, 0));
            ground.AddComponent<BoxCollider>().size = new Vector3(43, 0.55f, 24);
            Box("Diorama Plinth Shadow", land, new Vector3(0, -1.72f, 0), new Vector3(40, .17f, 21), Hex(0x37463D));
            Road(land, "Path Shoulder", 2.35f, 0.132f, Grass * .78f);
            Road(land, "Golden Earth Path", 1.95f, 0.15f, ActiveLevel != null ? ActiveLevel.pathColor : Hex(0xAD935F));
            Road(land, "Path Center", 1.15f, 0.155f, ActiveLevel != null ? ActiveLevel.pathCenterColor : Hex(0xB9A16D));

            for (int i = 0; i < 100; i++)
            {
                Vector3 p = new Vector3(Random.Range(-20f, 20f), .15f, Random.Range(-10.8f, 10.8f));
                if (DistanceToRoute(p) < 3.2f || CloseToSite(p, 2.15f) || Vector3.Distance(p, CampPosition) < 3.6f || Vector3.Distance(p, HeroStart) < 2.2f) continue;
                Tree(land, p, Random.Range(.65f, 1.22f), i % 4 != 0);
            }
            for (int i = 0; i < 80; i++)
            {
                Vector3 p = new Vector3(Random.Range(-20.5f, 20.5f), .15f, Random.Range(-11.2f, 11.2f));
                if (DistanceToRoute(p) < 1.3f || CloseToSite(p, 1.6f)) continue;
                if (i % 4 == 0) Rock(land, p, Random.Range(.35f, .85f));
                else if (i % 3 == 0) Flowers(land, p);
                else Bush(land, p, Random.Range(.3f, .65f));
            }
            for (int n = 0; n < Route.Length - 1; n++)
            {
                Vector3 d = Route[n + 1] - Route[n];
                Vector3 edge = Vector3.Cross(d.normalized, Vector3.up) * 1.12f;
                for (float t = .1f; t < .95f; t += .19f)
                {
                    Vector3 p = Vector3.Lerp(Route[n], Route[n + 1], t);
                    p.y = .17f;
                    if (Random.value > .3f) Rock(land, p + edge, .15f);
                    if (Random.value > .5f) Rock(land, p - edge, .13f);
                    Box("Worn Paving", land, p + new Vector3(Random.Range(-.3f, .3f), -.02f, Random.Range(-.3f, .3f)), new Vector3(.26f, .025f, .36f), Hex(0xB99B65), Random.Range(0, 180));
                }
            }
            Portal(land, Route[0] + new Vector3(-.65f, 0, 0));
            Castle(land, Route[Route.Length - 1] + new Vector3(-1.3f, 0, -.15f));
            NeutralCamp(land, CampPosition);
            Sign(land, new Vector3(-17.8f, .2f, 4.8f), -18);
            Sign(land, new Vector3(8.8f, .2f, -8.5f), 25);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2;
                Rock(land, new Vector3(-19.5f + i * 5.4f, -.85f, -11.5f + Mathf.Sin(a) * .25f), .45f);
            }
            Random.state = previousRandom;
        }

        static void BuildLighting(Transform parent)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ActiveLevel != null && ActiveLevel.biome == DefenseBiome.Winter ? new Color(.28f,.34f,.4f) : new Color(.27f, .32f, .32f);
            RenderSettings.ambientIntensity = .72f;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 100;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            GameObject sun = Root("Late Afternoon Sun", parent, new Vector3(0, 12, 0));
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = ActiveLevel != null && ActiveLevel.biome == DefenseBiome.Winter ? new Color(.85f,.94f,1f) : ActiveLevel != null && ActiveLevel.biome == DefenseBiome.Autumn ? new Color(1f,.9f,.72f) : new Color(1f, .95f, .85f);
            light.intensity = .85f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .72f;
            light.shadowBias = .035f;
            RenderSettings.sun = light;
            GameObject camera = Root("Main Camera", parent, Vector3.zero);
            camera.tag = "MainCamera";
            Vector3 target = new Vector3(0, 0, -3.8f);
            camera.transform.position = target + new Vector3(0, 36, -25);
            camera.transform.LookAt(target);
            Camera view = camera.AddComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = 15.8f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = ActiveLevel != null ? ActiveLevel.backgroundColor : Hex(0x182E34);
            view.nearClipPlane = .1f;
            view.farClipPlane = 150;
            view.allowHDR = true;
            camera.AddComponent<AudioListener>();
        }

        static Vector3[] IslandOutline(float scale, float y)
        {
            return new[] { new Vector3(-18.5f * scale,y,-12 * scale), new Vector3(18.5f*scale,y,-12*scale), new Vector3(21.5f*scale,y,-9*scale), new Vector3(21.5f*scale,y,9*scale), new Vector3(18.5f*scale,y,12*scale), new Vector3(-18.5f*scale,y,12*scale), new Vector3(-21.5f*scale,y,9*scale), new Vector3(-21.5f*scale,y,-9*scale) };
        }

        static void IslandLayer(Transform parent, string name, float bottomY, float topY, float bottomScale, float topScale, Color color)
        {
            Vector3[] bottom = IslandOutline(bottomScale, bottomY);
            Vector3[] top = IslandOutline(topScale, topY);
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                int j = (i + 1) % 8;
                int s = vertices.Count;
                vertices.Add(bottom[i]); vertices.Add(top[i]); vertices.Add(bottom[j]); vertices.Add(top[j]);
                triangles.AddRange(new[] {s,s+1,s+2,s+2,s+1,s+3});
            }
            MeshObject(name, parent, vertices.ToArray(), triangles.ToArray(), color);
        }

        static void IslandTop(Transform parent, Color color)
        {
            Vector3[] outline = IslandOutline(1, .12f);
            List<Vector3> verts = new List<Vector3> { new Vector3(0,.12f,0) };
            verts.AddRange(outline);
            List<int> indices = new List<int>();
            for (int i=0; i<8; i++) indices.AddRange(new[] {0,(i+1)%8+1,i+1});
            MeshObject("Meadow Surface",parent,verts.ToArray(),indices.ToArray(),color);
        }

        static GameObject MeshObject(string name, Transform parent, Vector3[] vertices, int[] triangles, Color color)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject go = Root(name, parent, Vector3.zero);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
            return go;
        }

        static void Road(Transform parent, string name, float width, float y, Color color)
        {
            Vector3[] verts = new Vector3[Route.Length * 2];
            List<int> triangles = new List<int>();
            for (int i = 0; i < Route.Length; i++)
            {
                Vector3 direction = (i == 0 ? Route[1] - Route[0] : i == Route.Length - 1 ? Route[i] - Route[i - 1] : (Route[i + 1] - Route[i]).normalized + (Route[i] - Route[i - 1]).normalized).normalized;
                Vector3 edge = Vector3.Cross(direction, Vector3.up).normalized * width * .5f;
                Vector3 p = Route[i]; p.y = y;
                verts[i*2] = p - edge; verts[i*2+1] = p + edge;
                if (i < Route.Length-1)
                {
                    int s = i*2;
                    triangles.AddRange(new[] {s,s+1,s+2,s+1,s+3,s+2});
                }
            }
            MeshObject(name,parent,verts,triangles.ToArray(),color);
        }

        static float DistanceToRoute(Vector3 p)
        {
            float best = float.MaxValue;
            p.y = 0;
            for(int i=0;i<Route.Length-1;i++)
            {
                Vector3 a=Route[i]; a.y=0;
                Vector3 b=Route[i+1]; b.y=0;
                Vector3 d=b-a;
                float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);
                best=Mathf.Min(best,Vector3.Distance(p,a+d*t));
            }
            return best;
        }

        static bool CloseToSite(Vector3 p,float distance)
        {
            foreach(Vector3 site in BuildSites) if(Vector3.Distance(p,site)<distance) return true;
            return false;
        }

        static void Tree(Transform parent,Vector3 p,float size,bool pine)
        {
            Transform tree=Root(pine?"Fir Tree":"Oak Tree",parent,p).transform;
            tree.localScale=Vector3.one*size;
            Frustum("Trunk",tree,new Vector3(0,0,0),.19f,.13f,1.5f,Wood,6);
            if(pine)
            {
                Frustum("Lower Needles",tree,new Vector3(0,.85f,0),1.15f,0,1.9f,FoliageDark,7);
                Frustum("Middle Needles",tree,new Vector3(0,1.65f,0),.91f,0,1.7f,FoliageMid,7);
                Frustum("Crown",tree,new Vector3(0,2.4f,0),.62f,0,1.45f,FoliageLight,7);
            }
            else
            {
                Frustum("Bough A",tree,new Vector3(-.3f,1.15f,0),.25f,.13f,.8f,Wood,6).transform.localRotation=Quaternion.Euler(0,0,35);
                Facet("Oak Crown",tree,new Vector3(0,2.1f,0),new Vector3(2.6f,2.0f,2.4f),FoliageMid);
                Frustum("Angular Crown",tree,new Vector3(.55f,1.65f,.2f),1.1f,.6f,1.4f,FoliageLight,6);
            }
            Frustum("Root Bed",tree,Vector3.zero,.39f,.2f,.1f,Grass*.85f,7);
        }

        static void Rock(Transform parent,Vector3 p,float size)
        {
            GameObject rock=Frustum("River Stone",parent,p,size,size*.6f,size*.65f,Random.value>.5f?Stone:StoneDark,5);
            rock.transform.localRotation=Quaternion.Euler(Random.Range(-10,10),Random.Range(0,360),Random.Range(-12,12));
        }

        static void Bush(Transform parent,Vector3 p,float size)
        {
            Frustum("Fern",parent,p,size,.08f,size*.7f,FoliageDark,5);
            Frustum("Fern Leaf",parent,p+new Vector3(size*.4f,0,0),size*.65f,0,size*.8f,FoliageLight,5);
        }

        static void Flowers(Transform parent,Vector3 p)
        {
            for(int j=0;j<3;j++)
            {
                Vector3 q=p+new Vector3(j*.18f,0,Mathf.Sin(j*3)*.18f);
                Frustum("Stem",parent,q,.025f,.018f,.25f,Hex(0x457544),5);
                Frustum("Meadow Flower",parent,q+Vector3.up*.24f,.09f,.035f,.09f,j%2==0?Hex(0xF2D889):Hex(0xD99CAC),5);
            }
        }

        public static GameObject CreatePad(Transform parent,Vector3 pos,int index)
        {
            GameObject pad=Root("Build Site "+(index+1),parent,pos);
            Frustum("Foundation",pad.transform,Vector3.zero,1.12f,1.04f,.12f,Hex(0x9A9C7A),12);
            Frustum("Build Surface",pad.transform,new Vector3(0,.12f,0),.93f,.93f,.035f,Hex(0x969B87),12);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                Box("Foundation Stone",pad.transform,new Vector3(Mathf.Sin(a)*.95f,.17f,Mathf.Cos(a)*.95f),new Vector3(.36f,.2f,.29f),Hex(0xA9B0A2),a*Mathf.Rad2Deg);
            }
            Box("Build Marker H",pad.transform,new Vector3(0,.18f,0),new Vector3(.65f,.035f,.14f),Hex(0xA3B08A));
            Box("Build Marker V",pad.transform,new Vector3(0,.18f,0),new Vector3(.14f,.035f,.65f),Hex(0xA3B08A));
            SphereCollider collider=pad.AddComponent<SphereCollider>();
            collider.center=new Vector3(0,.3f,0); collider.radius=1.08f;
            return pad;
        }

        public static GameObject CreateTower(Transform parent,Vector3 pos,int kind,int level)
        {
            GameObject tower=Root((kind==0?"Ranger Tower":kind==1?"Frost Spire":"Bombard Tower")+" L"+level,parent,pos);
            Transform t=tower.transform;
            Frustum("Stone Footing",t,Vector3.zero,1.05f,.94f,.25f,StoneDark,10);
            Frustum("Round Tower",t,new Vector3(0,.2f,0),.73f,.67f,1.45f,Stone,10);
            for(int row=0;row<3;row++)
            {
                float y=.5f+row*.42f;
                for(int j=0;j<5;j++)
                {
                    float a=(j*72+row*36)*Mathf.Deg2Rad;
                    Box("Masonry Joint",t,new Vector3(Mathf.Sin(a)*.693f,y,Mathf.Cos(a)*.693f),new Vector3(.19f,.03f,.035f),StoneDark,a*Mathf.Rad2Deg);
                }
            }
            Frustum("Top Cornice",t,new Vector3(0,1.55f,0),.82f,.82f,.22f,StoneLight,10);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;
                Box("Battlement",t,new Vector3(Mathf.Sin(a)*.67f,1.88f,Mathf.Cos(a)*.67f),new Vector3(.35f,.37f,.3f),StoneLight,a*Mathf.Rad2Deg);
            }
            Color accent=kind==0?Teal:kind==1?Blue:Hex(0xF3A261);
            Box("Banner",t,new Vector3(0,.94f,-.73f),new Vector3(.46f,.67f,.04f),accent);
            Box("Banner Mark",t,new Vector3(0,.94f,-.76f),new Vector3(.1f,.35f,.02f),Gold);
            if(kind==0)
            {
                Frustum("Timber Support",t,new Vector3(0,1.7f,0),.19f,.15f,.8f,Wood,7);
                Box("Crossbow",t,new Vector3(0,2.35f,0),new Vector3(.18f,.18f,1.1f),WoodLight);
                Box("Bow Arms",t,new Vector3(0,2.35f,.25f),new Vector3(1.15f,.12f,.13f),Wood,-8);
                Box("Arrow",t,new Vector3(0,2.42f,.35f),new Vector3(.04f,.04f,1.3f),StoneLight);
                Frustum("Arrowhead",t,new Vector3(0,2.42f,1.02f),.1f,0,.22f,StoneLight,4).transform.localRotation=Quaternion.Euler(90,0,0);
            }
            else if(kind==1)
            {
                Frustum("Arcane Plinth",t,new Vector3(0,1.76f,0),.44f,.28f,.43f,StoneDark,8);
                GameObject crystal=Frustum("Frost Crystal",t,new Vector3(0,2.05f,0),.39f,0,.96f,Blue,5);
                crystal.GetComponent<Renderer>().sharedMaterial=Material(Blue,true);
                Frustum("Crystal Base",t,new Vector3(0,1.9f,0),.17f,.39f,.2f,Hex(0xB7EFF0),5);
                for(int i=0;i<3;i++)
                {
                    float a=i*Mathf.PI*2/3;
                    Frustum("Crystal Shard",t,new Vector3(Mathf.Sin(a)*.45f,1.87f,Mathf.Cos(a)*.45f),.15f,0,.48f,Hex(0xA0E0F0),5);
                }
            }
            else
            {
                Box("Gun Carriage",t,new Vector3(0,1.97f,0),new Vector3(.95f,.38f,.65f),Wood);
                GameObject barrel=Frustum("Iron Cannon",t,new Vector3(0,2.05f,-.32f),.27f,.33f,1.25f,Hex(0x354D59),10);
                barrel.transform.localRotation=Quaternion.Euler(75,0,0);
                GameObject muzzle=Frustum("Cannon Muzzle",t,new Vector3(0,2.37f,.88f),.35f,.35f,.12f,StoneDark,10);
                muzzle.transform.localRotation=Quaternion.Euler(75,0,0);
                Frustum("Cannonball",t,new Vector3(.62f,1.78f,.35f),.18f,.1f,.3f,Hex(0x354D59),7);
            }
            for(int i=1;i<level;i++) Orb(t,new Vector3((i-1)*.22f-.11f,.48f,-.77f),.07f,Gold);
            CapsuleCollider collider=tower.AddComponent<CapsuleCollider>();
            collider.center=new Vector3(0,1.3f,0);collider.height=2.8f;collider.radius=.88f;
            return tower;
        }

        public static GameObject CreateUnit(Transform parent,Vector3 pos,int kind,bool hero=false)
        {
            if (!hero && kind >= 5 && kind <= 8) return CreateSpecialEnemy(parent, pos, kind);
            string unitName=hero?"Warden":EnemyCatalog.Get(kind).displayName;
            GameObject unit=Root(unitName,parent,pos);
            Transform t=unit.transform;
            float scale=hero?1.1f:kind==2?1.3f:kind==3?1.85f:kind==4?1.65f:.83f;
            Transform model=Root("Model",t,Vector3.zero).transform;
            model.localScale=Vector3.one*scale;
            Color skin=hero?Hex(0xEAC39A):kind==1?Hex(0xDDD6B9):kind==3?Hex(0x9BAA72):kind==4?Hex(0xACA18B):Hex(0x82AD5C);
            Color armor=hero?Hex(0x4B788D):kind==1?Hex(0x6C7076):kind==3?Hex(0x71615B):kind==4?Hex(0x65594D):Hex(0x875A48);
            Box("Left Boot",model,new Vector3(-.18f,.16f,.04f),new Vector3(.25f,.32f,.37f),StoneDark);
            Box("Right Boot",model,new Vector3(.18f,.16f,.04f),new Vector3(.25f,.32f,.37f),StoneDark);
            Frustum("Body",model,new Vector3(0,.3f,0),.3f,.39f,.65f,armor,6);
            Box("Belt",model,new Vector3(0,.48f,0),new Vector3(.65f,.12f,.46f),Wood);
            Box("Belt Buckle",model,new Vector3(0,.48f,.245f),new Vector3(.13f,.12f,.04f),Gold);
            Frustum("Head",model,new Vector3(0,1.03f,0),.28f,.23f,.4f,skin,7);
            Box("Left Eye",model,new Vector3(-.11f,1.25f,.235f),new Vector3(.065f,.07f,.04f),Hex(0x26363A));
            Box("Right Eye",model,new Vector3(.11f,1.25f,.235f),new Vector3(.065f,.07f,.04f),Hex(0x26363A));
            Facet("Left Shoulder",model,new Vector3(-.39f,.9f,0),new Vector3(.4f,.36f,.41f),hero?StoneLight:armor);
            Facet("Right Shoulder",model,new Vector3(.39f,.9f,0),new Vector3(.4f,.36f,.41f),hero?StoneLight:armor);
            Box("Left Arm",model,new Vector3(-.43f,.65f,.07f),new Vector3(.22f,.48f,.25f),skin);
            Box("Right Arm",model,new Vector3(.43f,.65f,.07f),new Vector3(.22f,.48f,.25f),skin);
            if(hero)
            {
                Frustum("Helmet",model,new Vector3(0,1.36f,0),.31f,.2f,.2f,StoneLight,7);
                Box("Helmet Crest",model,new Vector3(0,1.6f,-.03f),new Vector3(.1f,.3f,.42f),Teal);
                MeshObject("Warden Cape",model,new[]{new Vector3(-.3f,.95f,-.22f),new Vector3(.3f,.95f,-.22f),new Vector3(-.43f,.12f,-.43f),new Vector3(.43f,.12f,-.43f)},new[]{0,1,2,1,3,2,2,1,0,2,3,1},Hex(0x2C9183));
                Box("Sword Grip",model,new Vector3(.47f,.5f,.34f),new Vector3(.1f,.1f,.36f),Wood);
                Box("Sword Guard",model,new Vector3(.47f,.5f,.49f),new Vector3(.35f,.1f,.08f),Gold);
                Box("Sword Blade",model,new Vector3(.47f,.5f,.91f),new Vector3(.13f,.07f,.83f),Hex(0xDCF1E9));
                Frustum("Sword Tip",model,new Vector3(.47f,.5f,1.32f),.085f,0,.2f,Hex(0xDCF1E9),4).transform.localRotation=Quaternion.Euler(90,45,0);
                Box("Shield",model,new Vector3(-.47f,.65f,.34f),new Vector3(.53f,.67f,.14f),Hex(0x284D5C));
                Box("Shield Crest",model,new Vector3(-.47f,.65f,.42f),new Vector3(.08f,.48f,.04f),Gold);
            }
            else if(kind==1)
            {
                Box("Jaw",model,new Vector3(0,1.03f,.12f),new Vector3(.25f,.1f,.27f),skin);
                for(int i=0;i<3;i++)Box("Rib",model,new Vector3(0,.62f+i*.12f,.28f),new Vector3(.4f,.045f,.035f),skin);
                Box("Rusty Sword",model,new Vector3(.45f,.56f,.66f),new Vector3(.12f,.08f,.9f),Stone);
                Frustum("Steel Helmet",model,new Vector3(0,1.43f,0),.32f,.15f,.15f,StoneDark,7);
            }
            else if(kind>=2)
            {
                Box("Club Handle",model,new Vector3(.49f,.6f,.47f),new Vector3(.14f,.14f,.8f),Wood);
                Box("Heavy Club",model,new Vector3(.49f,.6f,.97f),new Vector3(.42f,.42f,.42f),kind==4?Hex(0x5A8180):StoneDark);
                Frustum("Left Horn",model,new Vector3(-.22f,1.37f,0),.11f,0,.35f,Gold,5).transform.localRotation=Quaternion.Euler(0,0,35);
                Frustum("Right Horn",model,new Vector3(.22f,1.37f,0),.11f,0,.35f,Gold,5).transform.localRotation=Quaternion.Euler(0,0,-35);
                Box("Chest Plate",model,new Vector3(0,.84f,.3f),new Vector3(.5f,.3f,.08f),StoneDark);
            }
            else
            {
                Frustum("Goblin Hood",model,new Vector3(0,1.34f,0),.32f,0,.35f,Hex(0x704A3C),6);
                Frustum("Left Ear",model,new Vector3(-.23f,1.19f,0),.15f,0,.3f,skin,4).transform.localRotation=Quaternion.Euler(0,0,70);
                Frustum("Right Ear",model,new Vector3(.23f,1.19f,0),.15f,0,.3f,skin,4).transform.localRotation=Quaternion.Euler(0,0,-70);
                Box("Dagger",model,new Vector3(.45f,.57f,.57f),new Vector3(.09f,.06f,.65f),StoneLight);
            }
            SphereCollider hitbox=unit.AddComponent<SphereCollider>();
            hitbox.center=new Vector3(0,.8f*scale,0);hitbox.radius=.56f*scale;
            return unit;
        }

        static GameObject CreateSpecialEnemy(Transform parent, Vector3 position, int kind)
        {
            var definition = EnemyCatalog.Get(kind);
            GameObject unit = Root(definition.displayName, parent, position);
            Transform model = Root("Model", unit.transform, Vector3.zero).transform;
            float scale = kind == 6 ? 1.35f : kind == 7 ? .93f : kind == 8 ? .96f : .9f;
            model.localScale = Vector3.one * scale;
            if (kind == 5) BloodbladeModel(model);
            else if (kind == 6) StoneguardModel(model);
            else if (kind == 7) HexcasterModel(model);
            else MenderModel(model);
            SphereCollider hitbox = unit.AddComponent<SphereCollider>();
            hitbox.center = new Vector3(0, .85f * scale, 0);
            hitbox.radius = (kind == 6 ? .72f : .56f) * scale;
            return unit;
        }

        static void BloodbladeModel(Transform model)
        {
            Color red = Hex(0xB43C38), leather = Hex(0x552D32), skin = Hex(0xBC9C76);
            Box("Left Sprint Boot", model, new Vector3(-.2f,.17f,.08f), new Vector3(.2f,.34f,.4f), leather);
            Box("Right Sprint Boot", model, new Vector3(.2f,.17f,-.05f), new Vector3(.2f,.34f,.4f), leather);
            Frustum("Light Leather Vest", model, new Vector3(0,.33f,0), .24f,.33f,.62f,red,6);
            Box("Black Belt", model, new Vector3(0,.46f,0), new Vector3(.53f,.12f,.4f), leather);
            Frustum("Masked Face", model, new Vector3(0,1.01f,.01f), .25f,.22f,.35f,skin,6);
            Frustum("Crimson Hood", model, new Vector3(0,1.31f,-.03f), .31f,0,.35f,red,6);
            Box("Face Mask", model, new Vector3(0,1.1f,.22f), new Vector3(.39f,.16f,.09f), leather);
            for (int side=-1;side<=1;side+=2)
            {
                Box("Amber Eye", model, new Vector3(side*.1f,1.24f,.23f), new Vector3(.065f,.06f,.05f), Gold);
                Box("Light Arm", model, new Vector3(side*.36f,.71f,.13f), new Vector3(.19f,.43f,.23f), skin);
                Box("Crimson Bracer", model, new Vector3(side*.36f,.53f,.17f), new Vector3(.23f,.17f,.28f), red);
                Box("Blade Grip", model, new Vector3(side*.4f,.52f,.38f), new Vector3(.09f,.09f,.28f), leather);
                Box("Blade Guard", model, new Vector3(side*.4f,.52f,.51f), new Vector3(.26f,.08f,.06f), Gold);
                Box("Twin Curved Blade", model, new Vector3(side*.4f,.52f,.76f), new Vector3(.11f,.05f,.52f), Hex(0xE3D7CA));
                Frustum("Knife Point", model, new Vector3(side*.4f,.52f,1.03f), .075f,0,.18f,Hex(0xE3D7CA),4).transform.localRotation = Quaternion.Euler(90,45,0);
            }
            MeshObject("Split Red Scarf", model, new[] {
                new Vector3(-.12f,1.12f,-.21f),new Vector3(.12f,1.12f,-.21f),
                new Vector3(-.3f,.65f,-.65f),new Vector3(.22f,.73f,-.55f)
            }, new[] {0,1,2,1,3,2,2,1,0,2,3,1}, Hex(0xDD5C45));
        }

        static void StoneguardModel(Transform model)
        {
            Color plate = Hex(0x879CA8), edge = Hex(0xBAC6C3), seam = Hex(0x3A4C59), rune = Hex(0x71C7CF);
            Box("Heavy Left Foot", model, new Vector3(-.25f,.2f,.07f), new Vector3(.4f,.4f,.52f), seam);
            Box("Heavy Right Foot", model, new Vector3(.25f,.2f,.07f), new Vector3(.4f,.4f,.52f), seam);
            Frustum("Stone Torso", model, new Vector3(0,.33f,0), .41f,.49f,.8f,plate,6);
            Box("Broad Breastplate", model, new Vector3(0,.85f,.36f), new Vector3(.75f,.49f,.19f), edge);
            Box("Chest Rune", model, new Vector3(0,.85f,.465f), new Vector3(.14f,.34f,.035f), rune);
            Frustum("Stone Helm", model, new Vector3(0,1.16f,0), .36f,.3f,.45f,plate,6);
            Box("Helm Brow", model, new Vector3(0,1.46f,.27f), new Vector3(.62f,.12f,.14f), edge);
            Box("Dark Visor", model, new Vector3(0,1.31f,.3f), new Vector3(.5f,.12f,.07f), seam);
            for (int side=-1;side<=1;side+=2)
            {
                Orb(model, new Vector3(side*.13f,1.32f,.35f), .045f, rune);
                Box("Square Pauldron", model, new Vector3(side*.54f,1.01f,0), new Vector3(.48f,.42f,.62f), plate);
                Box("Armored Forearm", model, new Vector3(side*.58f,.66f,.1f), new Vector3(.32f,.5f,.39f), seam);
                Frustum("Shoulder Spike", model, new Vector3(side*.54f,1.2f,-.08f), .1f,0,.28f,edge,4);
            }
            Box("Tower Shield", model, new Vector3(-.64f,.8f,.44f), new Vector3(.7f,1.06f,.2f), seam);
            Box("Shield Face", model, new Vector3(-.64f,.8f,.55f), new Vector3(.56f,.91f,.07f), plate);
            Box("Shield Sigil", model, new Vector3(-.64f,.8f,.6f), new Vector3(.12f,.65f,.04f), rune);
            Box("Hammer Grip", model, new Vector3(.61f,.61f,.42f), new Vector3(.15f,.15f,.68f), Wood);
            Box("Stone Maul", model, new Vector3(.61f,.61f,.83f), new Vector3(.56f,.42f,.4f), edge);
        }

        static void HexcasterModel(Transform model)
        {
            Color robe = Hex(0x633E89), shade = Hex(0x352D53), trim = Hex(0xB598D7), skin = Hex(0xAE9FC0);
            Frustum("Long Violet Robe", model, new Vector3(0,.09f,0), .49f,.3f,.97f,robe,7);
            Frustum("Silver Hem", model, new Vector3(0,.08f,0), .5f,.46f,.1f,trim,7);
            Box("Robe Center Trim", model, new Vector3(0,.58f,.32f), new Vector3(.09f,.75f,.045f), trim);
            Frustum("Pale Face", model, new Vector3(0,1.08f,.02f), .23f,.2f,.31f,skin,6);
            Frustum("Tall Sorcerer Hood", model, new Vector3(0,1.35f,-.05f), .33f,.07f,.5f,shade,6);
            Box("Shadowed Face", model, new Vector3(0,1.23f,.215f), new Vector3(.32f,.24f,.075f), shade);
            Orb(model, new Vector3(-.08f,1.27f,.265f), .035f, trim);
            Orb(model, new Vector3(.08f,1.27f,.265f), .035f, trim);
            for (int side=-1;side<=1;side+=2)
                Frustum("Long Sleeve", model, new Vector3(side*.37f,.55f,.08f), .19f,.23f,.47f,robe,6);
            Frustum("Hex Staff", model, new Vector3(.5f,.1f,.25f), .055f,.05f,1.88f,Wood,6);
            Frustum("Crystal Cradle", model, new Vector3(.5f,1.92f,.25f), .19f,.3f,.17f,Gold,5);
            GameObject crystal = Frustum("Hex Crystal", model, new Vector3(.5f,2.08f,.25f), .22f,0,.4f,trim,5);
            crystal.GetComponent<Renderer>().sharedMaterial = Material(trim,true);
            Orb(model, new Vector3(-.47f,.72f,.38f), .13f, Hex(0xCF96F0));
        }

        static void MenderModel(Transform model)
        {
            Color bark = Hex(0x685C3E), green = Hex(0x508B63), leaf = Hex(0x8FCA8B), spore = Hex(0xBFE6A0);
            Box("Root Foot Left", model, new Vector3(-.2f,.16f,.07f), new Vector3(.28f,.3f,.43f), bark);
            Box("Root Foot Right", model, new Vector3(.2f,.16f,.07f), new Vector3(.28f,.3f,.43f), bark);
            Frustum("Moss Body", model, new Vector3(0,.27f,0), .34f,.39f,.72f,green,7);
            Frustum("Pale Mushroom Stem", model, new Vector3(0,1.02f,0), .24f,.2f,.39f,Hex(0xD3D3AC),6);
            Frustum("Broad Mushroom Cap", model, new Vector3(0,1.37f,0), .63f,.42f,.23f,leaf,8);
            Frustum("Mushroom Crown", model, new Vector3(0,1.6f,0), .42f,.05f,.22f,green,8);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Gentle Eye", model, new Vector3(side*.085f,1.22f,.2f), new Vector3(.06f,.07f,.05f), Hex(0x243E35));
                Frustum("Leaf Shoulder", model, new Vector3(side*.4f,.83f,0), .27f,0,.25f,leaf,5);
                Box("Twig Arm", model, new Vector3(side*.4f,.59f,.06f), new Vector3(.16f,.44f,.2f), bark);
            }
            Frustum("Healing Totem Staff", model, new Vector3(.47f,.13f,.24f), .055f,.05f,1.3f,bark,5);
            Orb(model, new Vector3(.47f,1.48f,.24f), .19f, spore);
            for(int i=0;i<3;i++)
            {
                float a=i*Mathf.PI*2/3;
                Orb(model, new Vector3(Mathf.Sin(a)*.35f,1.95f+i*.04f,Mathf.Cos(a)*.25f), .065f,spore);
            }
            Box("Medic Satchel", model, new Vector3(-.32f,.55f,-.28f), new Vector3(.4f,.38f,.3f), Wood);
            Box("Satchel Cross Vertical", model, new Vector3(-.32f,.55f,-.445f), new Vector3(.07f,.27f,.035f), spore);
            Box("Satchel Cross Horizontal", model, new Vector3(-.32f,.55f,-.445f), new Vector3(.23f,.07f,.035f), spore);
        }

        public static GameObject CreateLoot(Transform parent,Vector3 pos)
        {
            GameObject loot=Root("Dropped Equipment",parent,pos);
            Transform t=loot.transform;
            Frustum("Loot Pedestal",t,Vector3.zero,.42f,.35f,.08f,Gold,8);
            Frustum("Enchanted Relic",t,new Vector3(0,.23f,0),.2f,0,.5f,Teal,4).transform.localRotation=Quaternion.Euler(0,45,0);
            Ring(t,new Vector3(0,.01f,0),.5f,new Color(.5f,1,.8f),.045f);
            SphereCollider collider=loot.AddComponent<SphereCollider>();collider.center=Vector3.up*.4f;collider.radius=.65f;
            return loot;
        }

        static void Castle(Transform parent,Vector3 pos)
        {
            Transform castle=Root("Sanctuary Gate",parent,pos).transform;
            Frustum("Sanctuary Foundation",castle,new Vector3(0,0,0),2.35f,2.2f,.18f,StoneDark,8);
            Box("Gate House",castle,new Vector3(0,1.12f,0),new Vector3(2.8f,2.2f,1.7f),Stone);
            Box("Timber Gate",castle,new Vector3(0,.85f,-.89f),new Vector3(1.14f,1.6f,.12f),Wood);
            Box("Gate Lintel",castle,new Vector3(0,1.73f,-.94f),new Vector3(1.35f,.18f,.2f),StoneLight);
            for(int j=-1;j<=1;j+=2)
            {
                Transform tower=Root("Gate Tower",castle,new Vector3(j*1.6f,0,0)).transform;
                Frustum("Tower",tower,Vector3.zero,.75f,.7f,2.65f,Stone,8);
                Frustum("Turquoise Roof",tower,new Vector3(0,2.63f,0),.94f,0,1.15f,Hex(0x387D83),8);
                Box("Arrow Slit",tower,new Vector3(0,1.65f,-.72f),new Vector3(.12f,.48f,.035f),StoneDark);
                Box("Pennant Pole",tower,new Vector3(0,4.01f,0),new Vector3(.055f,.6f,.055f),Gold);
                Box("Pennant",tower,new Vector3(.23f,4.12f,0),new Vector3(.45f,.25f,.045f),Teal);
            }
            Frustum("Gate Roof",castle,new Vector3(0,2.22f,0),1.8f,.5f,.85f,Hex(0x4D8885),4).transform.localRotation=Quaternion.Euler(0,45,0);
            Orb(castle,new Vector3(0,1.8f,-1.05f),.13f,Gold);
            Ring(castle,new Vector3(0,.18f,0),2.6f,new Color(.4f,.85f,.72f),.08f);
        }

        static void Portal(Transform parent,Vector3 pos)
        {
            Transform portal=Root("Raider's Gate",parent,pos).transform;
            Frustum("Dark Foundation",portal,Vector3.zero,1.8f,1.65f,.16f,StoneDark,8);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Gate Pillar",portal,new Vector3(0,1.3f,side*1.12f),new Vector3(.65f,2.6f,.65f),StoneDark,-10*side);
                Frustum("Pillar Cap",portal,new Vector3(0,2.57f,side*1.12f),.55f,0,.55f,Hex(0x9B6F72),5);
                Orb(portal,new Vector3(0,2.94f,side*1.12f),.12f,Hex(0xDF9292));
            }
            Box("Gate Arch",portal,new Vector3(0,2.45f,0),new Vector3(.7f,.55f,2.6f),StoneDark);
            GameObject glow=Box("Portal Veil",portal,new Vector3(0,1.25f,0),new Vector3(.08f,2.0f,1.8f),Hex(0x7C536D));
            glow.GetComponent<Renderer>().sharedMaterial=Material(Hex(0x855875),true);
            for(int i=0;i<7;i++)Orb(portal,new Vector3(.12f,.45f+i*.25f,Mathf.Sin(i*2)*.5f),.055f,Hex(0xE5A6A0));
        }

        static void NeutralCamp(Transform parent,Vector3 pos)
        {
            Transform camp=Root("Ancient Grove",parent,pos).transform;
            Frustum("Camp Clearing",camp,Vector3.zero,2.5f,2.4f,.04f,Color.Lerp(Grass,FoliageLight,.35f),12);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                Vector3 p=new Vector3(Mathf.Sin(a)*2.1f,.04f,Mathf.Cos(a)*2.1f);
                Frustum("Rune Stone",camp,p,.27f,.18f,.95f,StoneDark,5);
                Orb(camp,p+Vector3.up*.72f,.07f,Gold);
            }
            Transform chest=Root("Guardian Treasure",camp,new Vector3(-1.0f,.05f,1.1f)).transform;
            Box("Chest",chest,new Vector3(0,.33f,0),new Vector3(.9f,.6f,.64f),Wood);
            Frustum("Chest Lid",chest,new Vector3(0,.6f,0),.48f,.38f,.24f,WoodLight,4).transform.localRotation=Quaternion.Euler(0,45,0);
            Box("Chest Band A",chest,new Vector3(-.28f,.34f,-.335f),new Vector3(.09f,.59f,.04f),Gold);
            Box("Chest Band B",chest,new Vector3(.28f,.34f,-.335f),new Vector3(.09f,.59f,.04f),Gold);
            Box("Chest Lock",chest,new Vector3(0,.45f,-.35f),new Vector3(.18f,.18f,.07f),Gold);
        }

        static void Sign(Transform parent,Vector3 pos,float angle)
        {
            Transform sign=Root("Trail Sign",parent,pos).transform;sign.localRotation=Quaternion.Euler(0,angle,0);
            Box("Sign Post",sign,new Vector3(0,.55f,0),new Vector3(.14f,1.1f,.14f),Wood);
            Box("Sign Board",sign,new Vector3(.13f,.95f,0),new Vector3(.9f,.35f,.12f),WoodLight);
            Box("Sign Notch",sign,new Vector3(.24f,.95f,-.075f),new Vector3(.3f,.045f,.04f),Gold);
        }
    }
}
