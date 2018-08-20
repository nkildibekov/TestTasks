﻿using System;

namespace ReplicatedSite.Models.SiteMap
{
    public interface ISiteMapNode
    {
        string ID { get; set; }
        string Label { get; set; }

        Func<bool> IsVisible { get; set; }
    }
}
