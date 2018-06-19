// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Rendering.Materials;
using Xenko.Assets.Models;
using Xenko.Assets.Textures;

namespace Xenko.Assets.Tests
{
    /*
        TODO: TO REWRITE WITH new AssetImportSession

        [TestFixture]
        public class AssetImportTest
        {
            public const string DirectoryTestBase = @"data\Xenko.Assets.Tests\";

            [TestFixtureSetUp]
            public void Initialize()
            {
            }

            [Test]
            public void TestImportTexture()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportTexture");
                DeleteDirectory(projectDir);

                var project = new Project { ProjectPath = projectDir + "/test.xkpkg" };
                var session = new ProjectSession(project);
                Import(project, "texture", Path.Combine(DirectoryTestBase, "Logo.png"));

                // Save the project
                var result = session.Save();
                Assert.IsFalse(result.HasErrors);

                Assert.True(File.Exists(projectDir + "/texture/logo.xktex"));

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/texture/logo.xktex");

                Assert.AreEqual("../../Logo.png", textureAsset.Source.FullPath);

                // Cleanup before exit
                DeleteDirectory(projectDir);
            }

            [Test]
            public void TestImportModelWithTextures()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithTextures");
                DeleteDirectory(projectDir);

                var project = new Project { ProjectPath = projectDir + "/test.xkpkg" };
                var session = new ProjectSession(project);
                Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));

                var result = session.Save();
                Assert.IsFalse(result.HasErrors);

                Assert.True(File.Exists(projectDir + "/model/factory_entity.xkentity"));

                var modelAsset = ContentSerializer.Load<PrefabAsset>(projectDir + "/model/factory_entity.xkentity");

                Assert.AreEqual("factory_model", modelAsset.Data.Name);

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.xktex");

                Assert.AreEqual("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

                // Cleanup before exit
                DeleteDirectory(projectDir);
            }

            [Test]
            public void TestImportModelWithMaterialAndTextures()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithMaterialAndTextures");
                DeleteDirectory(projectDir);

                var project = new Project();
                var session = new ProjectSession(project);
                project.ProjectPath = projectDir + "/test.xkpkg";
                Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));
                session.Save();

                // 2 materials, 1 model, 1 entity, 1 texture
                Assert.AreEqual(5, project.Assets.Count);

                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn1.xkmat"));
                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn2.xkmat"));
                Assert.True(File.Exists(projectDir + "/model/factory.xkm3d"));

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.xktex");

                Assert.AreEqual("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

                var materialBlinn1 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn1.xkmat");
                var textureVisitor = new MaterialTextureVisitor(materialBlinn1.Material);
                var allTexturesBlinn1 = textureVisitor.GetAllTextureValues();
                Assert.AreEqual(1, allTexturesBlinn1.Count);
                foreach (var texture in allTexturesBlinn1)
                    Assert.AreNotEqual(texture.Texture.Id, textureAsset.Id);

                var materialBlinn2 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn2.xkmat");
                textureVisitor = new MaterialTextureVisitor(materialBlinn2.Material);
                var allTexturesBlinn2 = textureVisitor.GetAllTextureValues();
                Assert.AreEqual(1, allTexturesBlinn2.Count);
                foreach (var texture in allTexturesBlinn2)
                    Assert.AreEqual(texture.Texture.Id, textureAsset.Id);

                var model = ContentSerializer.Load<ModelAsset>(projectDir + "/model/factory.xkm3d");


                // Cleanup before exit
                DeleteDirectory(projectDir);
            }

            [Test]
            public void TestImportModelWithMaterialAndTextures2()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithMaterialAndTextures");
                DeleteDirectory(projectDir);

                var project = new Project();
                var session = new ProjectSession(project);
                project.ProjectPath = projectDir + "/test.xkpkg";
                Import(project, "model", Path.Combine(DirectoryTestBase, "knight.fbx"));
                session.Save();

                Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_KINGHT.xkmat"));
                Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_KINGHT_iron.xkmat"));
                Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_SWORD1.xkmat"));

                // Cleanup before exit
                DeleteDirectory(projectDir);

                project = new Project();
                session = new ProjectSession(project);
                project.ProjectPath = projectDir + "/test.xkpkg";
                Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));
                session.Save();

                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn1.xkmat"));
                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn2.xkmat"));
                Assert.True(File.Exists(projectDir + "/model/factory.xkm3d"));

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.xktex");

                Assert.AreEqual("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

                var materialBlinn1 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn1.xkmat");
                var textureVisitor = new MaterialTextureVisitor(materialBlinn1.Material);
                foreach (var texture in textureVisitor.GetAllTextureValues())
                    Assert.AreNotEqual(texture.Texture.Id, textureAsset.Id);

                var materialBlinn2 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn2.xkmat");
                textureVisitor = new MaterialTextureVisitor(materialBlinn2.Material);
                foreach (var texture in textureVisitor.GetAllTextureValues())
                    Assert.AreEqual(texture.Texture.Id, textureAsset.Id);

                var model = ContentSerializer.Load<ModelAsset>(projectDir + "/model/factory.xkm3d");


                // Cleanup before exit
                DeleteDirectory(projectDir);
            }

            private static void DeleteDirectory(string directory)
            {
                try
                {
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, true);
                }
                catch (Exception) { }
            }

            /// <summary>
            /// Imports a raw asset from the specified asset file path using importers registered in <see cref="AssetImporterRegistry" />.
            /// </summary>
            /// <param name="projectRelativeDirectory">The directory relative to the project where this asset should be imported.</param>
            /// <param name="filePathToRawAsset">The file path to raw asset.</param>
            /// <exception cref="System.ArgumentNullException">filePathToRawAsset</exception>
            /// <exception cref="AssetException">Unable to find a registered importer for the specified file extension [{0}]</exception>
            private static void Import(Project project, UDirectory projectRelativeDirectory, string filePathToRawAsset)
            {
                if (projectRelativeDirectory == null) throw new ArgumentNullException("projectRelativeDirectory");
                if (filePathToRawAsset == null) throw new ArgumentNullException("filePathToRawAsset");

                if (projectRelativeDirectory.IsAbsolute)
                {
                    throw new ArgumentException("Project directory must be relative to project and not absolute", "projectRelativeDirectory");
                }

                // Normalize input path
                filePathToRawAsset = FileUtility.GetAbsolutePath(filePathToRawAsset);
                if (!File.Exists(filePathToRawAsset))
                {
                    throw new FileNotFoundException("Unable to find file [{0}]".ToFormat(filePathToRawAsset), filePathToRawAsset);
                }

                // Check that an importer was found
                IAssetImporter importer = AssetRegistry.FindImporterForFile(Path.GetExtension(filePathToRawAsset)).FirstOrDefault();
                if (importer == null)
                {
                    throw new AssetException("Unable to find a registered importer for the specified file extension [{0}]", filePathToRawAsset);
                }

                List<AssetItem> newAssets = importer.Import(filePathToRawAsset, importer.GetDefaultParameters(false)).ToList();

                // Remove any asset which already exists
                var newAssetLocations = new HashSet<UFile>(newAssets.Select(x => x.Location));
                project.Assets.RemoveWhere(x => newAssetLocations.Contains(x.Location));

                // Add imported assets to this project
                foreach (var assetReference in newAssets)
                {
                    project.Assets.Add(assetReference);
                }
            }
        }
     */
}
