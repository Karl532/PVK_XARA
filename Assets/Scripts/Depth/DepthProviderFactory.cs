public static class DepthProviderFactory
{
	public enum DeviceType {Quest3}

	public static IDepthProvider Create(DeviceType type)
	{
		switch (type)
		{
			case DeviceType.Quest3:
				return new Quest3DepthProvider();
			default:
				throw new System.ArgumentException("Unsupported device");
		}
	}
}