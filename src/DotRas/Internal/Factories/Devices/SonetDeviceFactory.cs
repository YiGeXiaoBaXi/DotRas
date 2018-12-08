﻿using DotRas.Devices;
using DotRas.Internal.Abstractions.Factories;

namespace DotRas.Internal.Factories.Devices
{
    internal class SonetDeviceFactory : IDeviceFactory
    {
        public RasDevice Create(string name)
        {
            return new Sonet(name);
        }
    }
}