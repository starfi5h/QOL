using FullSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;


namespace com.brokenmass.plugin.DSP.MultiBuild
{
    [Serializable]
    public class BeltCopy
    {
        [NonSerialized]
        public ItemProto itemProto;

        public int protoId;
        public int originalId = 0;

        public Vector2 cursorRelativePos = Vector3.zero;
        public int originalSegmentCount;

        public int altitude = 0;
        public int backInputId;
        public int leftInputId;
        public int rightInputId;
        public int outputId;

        public int connectedBuildingId;
        public int connectedBuildingSlot;
        public bool connectedBuildingIsOutput;
    }

    [Serializable]
    public class BuildingCopy
    {
        [Serializable]
        public class StationSetting
        {
            public int index;
            public int itemId;
            public int max;
            public ELogisticStorage localLogic;
            public ELogisticStorage remoteLogic;
        }

        [Serializable]
        public class SlotFilter
        {
            public int slotIndex;
            public int storageIdx;
        }

        [NonSerialized]
        public ItemProto itemProto;

        public int protoId;
        public int originalId = 0;

        public int originalSegmentCount;
        public Vector2 cursorRelativePos = Vector2.zero;
        
        public float cursorRelativeYaw = 0f;
        public int modelIndex = 0;

        public int recipeId;

        public List<StationSetting> stationSettings = new List<StationSetting>();
        public List<SlotFilter> slotFilters = new List<SlotFilter>();
    }

    [Serializable]
    public class InserterCopy
    {
        [NonSerialized]
        public ItemProto itemProto;

        public int protoId;
        public int originalId = 0;

        public int pickTarget;
        public int insertTarget;

        public int referenceBuildingId = 0;
        public Vector2 otherPosDelta;
        public int otherPosDeltaCount;

        public bool incoming;
        public int startSlot;
        public int endSlot;
        public Vector2 posDelta;
        public Vector2 pos2Delta;

        public int posDeltaCount;
        public int pos2DeltaCount;

        public Quaternion rot;
        public Quaternion rot2;
        public short pickOffset;
        public short insertOffset;
        public int filterId;
        public bool otherIsBelt;
    }

    // reduce Vector3 to an array of 3 digits integers to reduce blueprint size
    public class Vector3Converter : fsDirectConverter
    {
        public const float JSON_PRECISION = 100f;
        public override Type ModelType => typeof(Vector3);

        public override object CreateInstance(fsData data, Type storageType)
        {
            return new Vector3();
        }

        public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
        {
            var data = new List<fsData>
            {
                new fsData((int)Math.Round(((Vector3)instance).x * JSON_PRECISION)),
                new fsData((int)Math.Round(((Vector3)instance).y * JSON_PRECISION)),
                new fsData((int)Math.Round(((Vector3)instance).z * JSON_PRECISION))
            };


            serialized = new fsData(data);
            return fsResult.Success;
        }

        public override fsResult TryDeserialize(fsData serialized, ref object instance, Type storageType)
        {
            if (!serialized.IsList) return fsResult.Fail("Expected to be and array of floats " + serialized);
            var data = serialized.AsList;

            instance = new Vector3()
            {
                x = ((float)data[0].AsInt64) / JSON_PRECISION,
                y = ((float)data[1].AsInt64) / JSON_PRECISION,
                z = ((float)data[2].AsInt64) / JSON_PRECISION
            };



            return fsResult.Success;
        }
    }

    // reduce Vector2 to an array of 3 digits integers to reduce blueprint size
    public class Vector2Converter : fsDirectConverter
    {
        public const float JSON_PRECISION = 100f;
        public override Type ModelType => typeof(Vector2);

        public override object CreateInstance(fsData data, Type storageType)
        {
            return new Vector2();
        }
        
        public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
        {
            Vector2 converted = ((Vector2) instance).ToDegrees();
            var data = new List<fsData>
            {
                new fsData((int)Math.Round(converted.x * JSON_PRECISION)),
                new fsData((int)Math.Round(converted.y * JSON_PRECISION))
            };

            serialized = new fsData(data);
            return fsResult.Success;
        }

        public override fsResult TryDeserialize(fsData serialized, ref object instance, Type storageType)
        {
            if (!serialized.IsList) return fsResult.Fail("Expected to be and array of floats " + serialized);
            var data = serialized.AsList;

            instance = new Vector2()
            {
                x = data[0].AsInt64 / JSON_PRECISION,
                y = data[1].AsInt64 / JSON_PRECISION
            }.ToRadians();
            
            return fsResult.Success;
        }
    }

    // reduce Quaternion to an array of 5 digits integers to reduce blueprint size
    public class QuaternionConverter : fsDirectConverter
    {
        public const float JSON_PRECISION = 10000f;
        public override Type ModelType => typeof(Quaternion);

        public override object CreateInstance(fsData data, Type storageType)
        {
            return new Quaternion();
        }
        
        public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
        {
            Quaternion inst = (Quaternion) instance;
            var data = new List<fsData>
            {
                new fsData((int)Math.Round(inst.x * JSON_PRECISION)),
                new fsData((int)Math.Round(inst.y * JSON_PRECISION)),
                new fsData((int)Math.Round(inst.z * JSON_PRECISION)),
                new fsData((int)Math.Round(inst.w * JSON_PRECISION))
            };

            serialized = new fsData(data);
            return fsResult.Success;
        }

        public override fsResult TryDeserialize(fsData serialized, ref object instance, Type storageType)
        {
            if (!serialized.IsList) return fsResult.Fail("Expected to be and array of floats " + serialized);
            var data = serialized.AsList;

            instance = new Quaternion()
            {
                x = data[0].AsInt64 / JSON_PRECISION,
                y = data[1].AsInt64 / JSON_PRECISION,
                z = data[2].AsInt64 / JSON_PRECISION,
                w = data[3].AsInt64 / JSON_PRECISION
            };
            
            return fsResult.Success;
        }
    }

    public class BlueprintData
    {
        [NonSerialized]
        public string name = "";
        public int version = 2;
        public Vector2 referencePos = Vector3.zero;

        public List<BuildingCopy> copiedBuildings = new List<BuildingCopy>();
        public List<InserterCopy> copiedInserters = new List<InserterCopy>();
        public List<BeltCopy> copiedBelts = new List<BeltCopy>();

        public static BlueprintData Import(string input)
        {
            string unzipped;
            var name = "";

            try
            {
                List<string> segments = input.Split(':').ToList();
                var base64Data = segments.Last();

                if (segments.Count > 1)
                {
                    segments.RemoveAt(segments.Count - 1);
                    name = String.Join(":", segments.ToArray());
                }
                unzipped = Unzip(Convert.FromBase64String(base64Data));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while unzipping string: " + e.ToString());
                return null;
            }


            BlueprintData deserialized = null;
            try
            {
                fsSerializer serializer = new fsSerializer();
                serializer.AddConverter(new Vector3Converter());
                serializer.AddConverter(new Vector2Converter());
                serializer.AddConverter(new QuaternionConverter());

                fsData data = fsJsonParser.Parse(unzipped);

                serializer.TryDeserialize<BlueprintData>(data, ref deserialized).AssertSuccessWithoutWarnings();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to deserialise v2: " + e.ToString());
                return null;
            }

            foreach (var building in deserialized.copiedBuildings)
            {
                building.itemProto = LDB.items.Select((int)building.protoId);
            }
            foreach (var belt in deserialized.copiedBelts)
            {
                belt.itemProto = LDB.items.Select((int)belt.protoId);
            }
            foreach (var inserter in deserialized.copiedInserters)
            {
                inserter.itemProto = LDB.items.Select((int)inserter.protoId);
            }


            deserialized.name = name;
            return deserialized;
        }


        public string Export()
        {
            fsSerializer serializer = new fsSerializer();

            serializer.AddConverter(new Vector3Converter());
            serializer.AddConverter(new Vector2Converter());
            serializer.AddConverter(new QuaternionConverter());
            serializer.TrySerialize<BlueprintData>(this, out fsData data).AssertSuccessWithoutWarnings();

            string json = fsJsonPrinter.CompressedJson(data);
            return Convert.ToBase64String(Zip(json));
        }

        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        private static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        private static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}
