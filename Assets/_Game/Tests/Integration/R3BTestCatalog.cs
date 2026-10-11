#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Spotlight.Bootstrap;
using Spotlight.Contracts;
using Spotlight.Core.Data;
using Spotlight.Enemies.Definitions;
using UnityEditor;

namespace Spotlight.Tests.Integration
{
    // Every registration comes from the formal Catalog asset.
    // N05 drops are loaded through the actual public Catalog DropTables references.
    internal static class R3BTestCatalog
    {
        internal static CatalogData Build()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
            Assert.NotNull(asset);
            CatalogData data = asset.BuildCatalogData();
            CollectionAssert.IsEmpty(new GameCatalog(data).ValidateAll(), "All formal catalog references must resolve");
            return data;
        }
    }
}
#endif