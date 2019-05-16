// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.PackageExtraction;
using NuGet.Packaging.Signing;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;

namespace NuGet.Commands
{
    public class RestoreRequest
    {
        public static readonly int DefaultDegreeOfConcurrency = 16;

        private string _lockFilePath;

        private Lazy<LockFile> _lockFileLazy;

        public RestoreRequest(
            PackageSpec project,
            RestoreCommandProviders dependencyProviders,
            SourceCacheContext cacheContext,
            ClientPolicyContext clientPolicyContext,
            ILogger log)
        {

            CacheContext = cacheContext ?? throw new ArgumentNullException(nameof(cacheContext));
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Project = project ?? throw new ArgumentNullException(nameof(project));
            DependencyProviders = dependencyProviders ?? throw new ArgumentNullException(nameof(dependencyProviders));
            ClientPolicyContext = clientPolicyContext;

            ExternalProjects = new List<ExternalProjectReference>();
            CompatibilityProfiles = new HashSet<FrameworkRuntimePair>();
            PackagesDirectory = dependencyProviders.GlobalPackages.RepositoryRoot;
            IsLowercasePackagesDirectory = true;
            ProjectStyle = Project.RestoreMetadata?.ProjectStyle ?? ProjectStyle.Unknown;
            // Project.json is special cased to put assets file and generated .props and targets in the project folder.
            // Unknown is there mostly for test purposes.
            RestoreOutputPath = ProjectStyle == ProjectStyle.ProjectJson || ProjectStyle == ProjectStyle.Unknown
                ? Path.GetDirectoryName(Project.FilePath)
                : Project.RestoreMetadata.OutputPath;
            MSBuildProjectExtensionsPath = Project.RestoreMetadata?.OutputPath;
        }

        public DependencyGraphSpec DependencyGraphSpec { get; set; }

        public bool AllowNoOp { get; set; }

        public SourceCacheContext CacheContext { get; }

        public ILogger Log { get; set; }

        /// <summary>
        /// The project to perform the restore on
        /// </summary>
        public PackageSpec Project { get; }

        /// <summary>
        /// The directory in which to install packages
        /// </summary>
        public string PackagesDirectory { get; }

        /// <summary>
        /// Whether or not packages written and read from the global packages directory has
        /// lowercase ID and version folder names or original case.
        /// </summary>
        public bool IsLowercasePackagesDirectory { get; set; }

        /// <summary>
        /// A list of projects provided by external build systems (i.e. MSBuild)
        /// </summary>
        public IList<ExternalProjectReference> ExternalProjects { get; set; }

        /// <summary>
        /// The path to the lock file to read/write. If not specified, uses the file 'project.lock.json' in the same
        /// directory as the provided PackageSpec.
        /// </summary>
        public string LockFilePath
        {
            get => _lockFilePath;
            set
            {
                _lockFilePath = value;
                _lockFileLazy = string.IsNullOrWhiteSpace(value) ? null : new Lazy<LockFile>(() => LockFileUtilities.GetLockFile(_lockFilePath, Log));
            }
        }

        /// <summary>
        /// The existing lock file to use. If not specified, the lock file will be read from the <see cref="LockFilePath"/>
        /// (or, if that property is not specified, from the default location of the lock file, as specified in the
        /// description for <see cref="LockFilePath"/>)
        /// </summary>
        public LockFile ExistingLockFile
        {
            get { return _lockFileLazy?.Value; }
            set
            {
                _lockFileLazy = value == null ? null : new Lazy<LockFile>(() => value);
            }
        }

        /// <summary>
        /// The number of concurrent tasks to run during installs. Defaults to
        /// <see cref="DefaultDegreeOfConcurrency" />. Set this to '1' to
        /// run without concurrency.
        /// </summary>
        public int MaxDegreeOfConcurrency { get; set; } = DefaultDegreeOfConcurrency;

        /// <summary>
        /// Additional compatibility profiles to check compatibility with.
        /// </summary>
        public ISet<FrameworkRuntimePair> CompatibilityProfiles { get; }

        /// <summary>
        /// Lock file version format to output.
        /// </summary>
        /// <remarks>This defaults to the latest version.</remarks>
        public int LockFileVersion { get; set; } = LockFileFormat.Version;

        /// <summary>
        /// These Runtime Ids will be added to the graph in addition to the runtimes contained
        /// in project.json under runtimes.
        /// </summary>
        /// <remarks>RIDs are case sensitive.</remarks>
        public ISet<string> RequestedRuntimes { get; } = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// These Runtime Ids will be used if <see cref="RequestedRuntimes"/> and the project runtimes
        /// are both empty.
        /// </summary>
        /// <remarks>RIDs are case sensitive.</remarks>
        public ISet<string> FallbackRuntimes { get; } = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// This contains resources that are shared between project restores.
        /// This includes both remote and local package providers.
        /// </summary>
        public RestoreCommandProviders DependencyProviders { get; set; }

        /// <summary>
        /// Defines the paths and behavior for outputs
        /// </summary>
        public ProjectStyle ProjectStyle { get; }

        /// <summary>
        /// Restore output path
        /// </summary>
        public string RestoreOutputPath { get; }

        /// <summary>
        /// MSBuildProjectExtensionsPath
        /// </summary>
        public string MSBuildProjectExtensionsPath { get; }

        /// <summary>
        /// Compatibility options
        /// </summary>
        public bool ValidateRuntimeAssets { get; set; } = true;

        /// <summary>
        /// Display Errors and warnings as they occur
        /// </summary>
        public bool HideWarningsAndErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="Packaging.PackageSaveMode"/>. 
        /// </summary> 
        public PackageSaveMode PackageSaveMode { get; set; } = PackageSaveMode.Defaultv3;

        public XmlDocFileSaveMode XmlDocFileSaveMode { get; set; } = PackageExtractionBehavior.XmlDocFileSaveMode;

        public ClientPolicyContext ClientPolicyContext { get; }

        /// <remarks>
        /// This property should only be used to override the default verifier on tests.
        /// </remarks>
        internal IPackageSignatureVerifier SignedPackageVerifier { get; set; }

        public Guid ParentId { get; set;}

        public bool IsRestoreOriginalAction { get; set; } = true;

        public bool RestoreForceEvaluate { get; set; }
    }
}
