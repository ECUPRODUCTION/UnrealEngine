// Copyright 1998-2018 Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class nvTextureTools : ModuleRules
{
	public nvTextureTools(ReadOnlyTargetRules Target) : base(Target)
	{
		Type = ModuleType.External;

		string nvttPath = Target.UEThirdPartySourceDirectory + "nvTextureTools/nvTextureTools-2.0.8/";

		string nvttLibPath = nvttPath + "lib";

		PublicIncludePaths.Add(nvttPath + "src/src");

		if (Target.Platform == UnrealTargetPlatform.Win64)
		{
			nvttLibPath += ("/Win64/VS" + Target.WindowsPlatform.GetVisualStudioCompilerVersionName());
			PublicLibraryPaths.Add(nvttLibPath);

			PublicAdditionalLibraries.Add("nvtt_64.lib");

			PublicDelayLoadDLLs.Add("nvtt_64.dll");

			RuntimeDependencies.Add("$(EngineDir)/Binaries/ThirdParty/nvTextureTools/Win64/nvtt_64.dll");
		}
		else if (Target.Platform == UnrealTargetPlatform.Win32)
		{
			nvttLibPath += ("/Win32/VS" + Target.WindowsPlatform.GetVisualStudioCompilerVersionName());
			PublicLibraryPaths.Add(nvttLibPath);

			PublicAdditionalLibraries.Add("nvtt.lib");

			PublicDelayLoadDLLs.Add("nvtt.dll");

			RuntimeDependencies.Add("$(EngineDir)/Binaries/ThirdParty/nvTextureTools/Win32/nvtt.dll");
		}
		else if (Target.Platform == UnrealTargetPlatform.Mac)
		{
			PublicAdditionalLibraries.Add(nvttPath + "lib/Mac/libnvcore.dylib");
			PublicAdditionalLibraries.Add(nvttPath + "lib/Mac/libnvimage.dylib");
			PublicAdditionalLibraries.Add(nvttPath + "lib/Mac/libnvmath.dylib");
			PublicAdditionalLibraries.Add(nvttPath + "lib/Mac/libnvtt.dylib");
			PublicAdditionalLibraries.Add(nvttPath + "lib/Mac/libsquish.a");
		}
		else if (Target.IsInPlatformGroup(UnrealPlatformGroup.Unix))
		{
			string NvBinariesDir = Target.UEThirdPartyBinariesDirectory + "nvTextureTools/Linux/" + Target.Architecture;
			PrivateRuntimeLibraryPaths.Add(NvBinariesDir);

			PublicAdditionalLibraries.Add(NvBinariesDir + "/libnvcore.so");
			PublicAdditionalLibraries.Add(NvBinariesDir + "/libnvimage.so");
			PublicAdditionalLibraries.Add(NvBinariesDir + "/libnvmath.so");
			PublicAdditionalLibraries.Add(NvBinariesDir + "/libnvtt.so");

			RuntimeDependencies.Add(NvBinariesDir + "/libnvcore.so");
			RuntimeDependencies.Add(NvBinariesDir + "/libnvimage.so");
			RuntimeDependencies.Add(NvBinariesDir + "/libnvmath.so");
			RuntimeDependencies.Add(NvBinariesDir + "/libnvtt.so");
		}
	}
}

