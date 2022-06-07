using SmartIOT.Connector.Core.Conf;
using System.Collections.ObjectModel;

namespace SmartIOT.Connector.Core.Model
{
	public class Device
	{
		public DeviceConfiguration Configuration { get; }

		public DeviceStatus DeviceStatus { get; internal set; }
		public string Name => Configuration.Name;
		public string DeviceId => Configuration.DeviceId;
		public int ErrorCode { get; internal set; }
		public int ErrorCount { get; internal set; }
		public IList<Tag> Tags => new ReadOnlyCollection<Tag>(_tags);

		public bool IsPartialReadsEnabled => Configuration.IsPartialReadsEnabled;
		public int SinglePDUReadBytes { get; set; }
		public bool IsWriteOptimizationEnabled => Configuration.IsWriteOptimizationEnabled;
		public int SinglePDUWriteBytes { get; set; }
		public int PDULength { get; set; }

		private readonly IList<Tag> _tags = new List<Tag>();

		private int _weightMCMRead;
		private int _weightMCMWrite;

		public Device(DeviceConfiguration deviceConfiguration)
		{
			Configuration = deviceConfiguration;
			DeviceStatus = deviceConfiguration.Enabled ? DeviceStatus.UNINITIALIZED : DeviceStatus.DISABLED;

			foreach (var tagConfiguration in deviceConfiguration.Tags)
			{
				AddTag(new Tag(tagConfiguration));
			}
		}

		private void AddTag(Tag tag)
		{
			_tags.Add(tag);

			switch (tag.TagType)
			{
				case TagType.READ:
					_weightMCMRead = CalculateMCM(TagType.READ);
					break;
				case TagType.WRITE:
					_weightMCMWrite = CalculateMCM(TagType.WRITE);
					break;
			}
		}

		internal void IncrementOrReseDeviceErrorCode(int err)
		{
			ErrorCode = err;
			if (err != 0)
			{
				ErrorCount++;
				DeviceStatus = DeviceStatus.ERROR;
			}
			else
			{
				ErrorCount = 0;
				DeviceStatus = DeviceStatus.OK;
			}
		}

		internal void SetTagSynchronized(Tag tag, DateTime instant)
		{
			tag.LastDeviceSynchronization = instant;

			TagType type = tag.TagType;

			if (type == TagType.READ)
			{
				tag.Points += tag.Weight;

				int minPunti = GetMinPoints(type);
				while (minPunti > 0 && minPunti % GetWeightMCM(type) == 0)
				{
					foreach (Tag t in _tags)
					{
						if (t.TagType == type)
						{
							t.Points -= GetWeightMCM(type);
							if (t.Points < 0)
								t.Points = 0;
						}
					}
					minPunti = GetMinPoints(type);
				}

				bool riscalaturaPossibile = true;
				int maxPunti = int.MaxValue;

				while (riscalaturaPossibile && maxPunti > 0)
				{
					maxPunti = GetMaxPoints(type);
					List<Tag> list = GetTagsInRange(type, maxPunti - GetWeightMCM(type), maxPunti);

					int min = int.MaxValue;
					foreach (Tag t in list)
					{
						if (t.Points < min)
							min = t.Points;
					}

					if (min - GetWeightMCM(type) < 0)
					{
						riscalaturaPossibile = false;
						continue;
					}

					list = GetTagsInRange(type, min - GetWeightMCM(type), min);

					foreach (Tag t in list)
					{
						if (t.Points != min)
						{
							riscalaturaPossibile = false;
							break;
						}
					}

					if (riscalaturaPossibile)
					{
						foreach (Tag t in _tags)
						{
							if (t.Points >= min)
							{
								t.Points -= GetWeightMCM(type);
							}
							if (t.Points < 0)
								t.Points = 0;
						}
					}
				}
			}
		}

		private List<Tag> GetTagsInRange(TagType type, int minPoints, int maxPoints)
		{
			var list = new List<Tag>();
			foreach (Tag t in _tags)
			{
				if (t.TagType == type && t.Points >= minPoints && t.Points <= maxPoints)
					list.Add(t);
			}
			return list;
		}

		private int GetMaxPoints(TagType type)
		{
			int max = int.MinValue;
			foreach (Tag t in _tags)
			{
				if (t.TagType == type && t.Points > max)
					max = t.Points;
			}
			return max;
		}

		private int GetMinPoints(TagType type)
		{
			int min = int.MaxValue;
			foreach (Tag t in _tags)
			{
				if (t.TagType == type && t.Points < min)
					min = t.Points;
			}
			return min;
		}

		private int GetWeightMCM(TagType type)
		{
			if (type == TagType.READ)
				return _weightMCMRead;
			else
				return _weightMCMWrite;
		}

		protected int CalculateMCM(TagType type)
		{
			int max = int.MinValue;

			foreach (Tag t in _tags)
			{
				if (t.TagType == type && t.Weight > max)
					max = t.Weight;
			}

			bool mcmTrovato = false;
			while (!mcmTrovato)
			{
				mcmTrovato = true;

				foreach (Tag t in _tags)
				{
					if (t.TagType == type && max % t.Weight != 0)
					{
						mcmTrovato = false;
						break;
					}
				}

				if (mcmTrovato)
					break;

				max++;
			}

			return max;
		}
	}
}
