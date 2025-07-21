using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace BetterSleep.Core
{
	internal enum QueueType
	{
		upsert,
		delete,
	}

	internal sealed class ToDelete : Savable { }

	internal sealed class QueueEntry
	{
		public QueueType QueueType { get; set; } = QueueType.upsert;
		public Savable Data { get; set; }
	}

	[DataContract]
	[KnownType("GetKnownTypes")]
	internal abstract class Savable
	{
		[DataMember] public int Id { get; set; }
		[DataMember] public Vector3? Position { get; set; } = null;
		[DataMember] public string Type { get; set; }

		public Savable()
		{
			Type = GetType().Name;
		}

		private static IEnumerable<Type> _knownTypes;
		private static IEnumerable<Type> GetKnownTypes()
		{
			if (_knownTypes == null)
				_knownTypes = Assembly.GetExecutingAssembly()
										.GetTypes()
										.Where(t => typeof(Savable).IsAssignableFrom(t) && t.Name != "ToDelete")
										.ToList();
			return _knownTypes;
		}
	}

	[DataContract]
	internal sealed class SaveData
	{
		[DataMember] public List<Savable> Data { get; set; }
	}

	[DataContract]
	internal class TirednessData : Savable
	{
		[DataMember] public float Tiredness { get; set; }
		[DataMember] public float LastSleepTime { get; set; }
		[DataMember] public float LastTirednessUpdate { get; set; }
		[DataMember] public float LastSleepQuality { get; set; }

		public TirednessData(float tiredness, float lastSleepTime, float lastTirednessUpdate, float lastSleepQuality)
		{
			Id = 0;
			Tiredness = tiredness;
			LastSleepTime = lastSleepTime;
			LastTirednessUpdate = lastTirednessUpdate;
			LastSleepQuality = lastSleepQuality;
		}
	}
}
