// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Rendering.Materials;
using Stride.Assets.Models;
using Stride.Assets.Textures;

namespace Stride.Assets.Tests
{
    /*
        TODO: TO REWRITE WITH new AssetImportSession

        public class AssetImportTest
        {
            public const string DirectoryTestBase = @"data\Stride.Assets.Tests\";

            [TestFixtureSetUp]
            public void Initialize()
            {
            }

            [Fact]
            public void TestImportTexture()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportTexture");
                DeleteDirectory(projectDir);

                var project = new Project { ProjectPath = projectDir + "/test.sdpkg" };
                var session = new ProjectSession(project);
                Import(project, "texture", Path.Combine(DirectoryTestBase, "Logo.png"));

                // Save the project
                var result = session.Save();
                Assert.False(result.HasErrors);

                Assert.True(File.Exists(projectDir + "/texture/logo.sdtex"));

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/texture/logo.sdtex");

                Assert.Equal("../../Logo.png", textureAsset.Source.FullPath);

                // Cleanup before exit
                DeleteDirectory(projectDir);
            }

            [Fact]
            public void TestImportModelWithTextures()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithTextures");
                DeleteDirectory(projectDir);

                var project = new Project { ProjectPath = projectDir + "/test.sdpkg" };
                var session = new ProjectSession(project);
                Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));

                var result = session.Save();
                Assert.False(result.HasErrors);

                Assert.True(File.Exists(projectDir + "/model/factory_entity.sdentity"));

                var modelAsset = ContentSerializer.Load<PrefabAsset>(projectDir + "/model/factory_entity.sdentity");

                Assert.Equal("factory_model", modelAsset.Data.Name);

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.sdtex");

                Assert.Equal("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

                // Cleanup before exit
                DeleteDirectory(projectDir);
            }

            [Fact]
            public void TestImportModelWithMaterialAndTextures()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithMaterialAndTextures");
                DeleteDirectory(projectDir);

                var project = new Project();
                var session = new ProjectSession(project);
                project.ProjectPath = projectDir + "/test.sdpkg";
                Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));
                session.Save();

                // 2 materials, 1 model, 1 entity, 1 texture
                Assert.Equal(5, project.Assets.Count);

                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn1.sdmat"));
                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn2.sdmat"));
                Assert.True(File.Exists(projectDir + "/model/factory.sdm3d"));

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.sdtex");

                Assert.Equal("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

                var materialBlinn1 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn1.sdmat");
                var textureVisitor = new MaterialTextureVisitor(materialBlinn1.Material);
                var allTexturesBlinn1 = textureVisitor.GetAllTextureValues();
                Assert.Equal(1, allTexturesBlinn1.Count);
                foreach (var texture in allTexturesBlinn1)
                    Assert.NotEqual(texture.Texture.Id, textureAsset.Id);

                var materialBlinn2 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn2.sdmat");
                textureVisitor = new MaterialTextureVisitor(materialBlinn2.Material);
                var allTexturesBlinn2 = textureVisitor.GetAllTextureValues();
                Assert.Equal(1, allTexturesBlinn2.Count);
                foreach (var texture in allTexturesBlinn2)
                    Assert.Equal(texture.Texture.Id, textureAsset.Id);

                var model = ContentSerializer.Load<ModelAsset>(projectDir + "/model/factory.sdm3d");


                // Cleanup before exit
                DeleteDirectory(projectDir);
            }

            [Fact]
            public void TestImportModelWithMaterialAndTextures2()
            {
                var projectDir = Path.Combine(DirectoryTestBase, "TestImportModelWithMaterialAndTextures");
                DeleteDirectory(projectDir);

                var project = new Project();
                var session = new ProjectSession(project);
                project.ProjectPath = projectDir + "/test.sdpkg";
                Import(project, "model", Path.Combine(DirectoryTestBase, "knight.fbx"));
                session.Save();

                Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_KINGHT.sdmat"));
                Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_KINGHT_iron.sdmat"));
                Assert.True(File.Exists(projectDir + "/model/knight_material_c100_chr_ch00_Knight_SWORD1.sdmat"));

                // Cleanup before exit
                DeleteDirectory(projectDir);

                project = new Project();
                session = new ProjectSession(project);
                project.ProjectPath = projectDir + "/test.sdpkg";
                Import(project, "model", Path.Combine(DirectoryTestBase, "factory.fbx"));
                session.Save();

                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn1.sdmat"));
                Assert.True(File.Exists(projectDir + "/model/factory_material_blinn2.sdmat"));
                Assert.True(File.Exists(projectDir + "/model/factory.sdm3d"));

                var textureAsset = ContentSerializer.Load<TextureAsset>(projectDir + "/model/factory_TX-Factory_Ground.sdtex");

                Assert.Equal("../../TX-Factory_Ground.dds", textureAsset.Source.FullPath);

                var materialBlinn1 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn1.sdmat");
                var textureVisitor = new MaterialTextureVisitor(materialBlinn1.Material);
                foreach (var texture in textureVisitor.GetAllTextureValues())
                    Assert.NotEqual(texture.Texture.Id, textureAsset.Id);

                var materialBlinn2 = ContentSerializer.Load<MaterialAsset>(projectDir + "/model/factory_material_blinn2.sdmat");
                textureVisitor = new MaterialTextureVisitor(materialBlinn2.Material);
                foreach (var texture in textureVisitor.GetAllTextureValues())
                    Assert.Equal(texture.Texture.Id, textureAsset.Id);

                var model = ContentSerializer.Load<ModelAsset>(projectDir + "/model/factory.sdm3d");


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
