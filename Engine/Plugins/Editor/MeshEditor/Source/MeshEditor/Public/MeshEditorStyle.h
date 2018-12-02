// Copyright 1998-2018 Epic Games, Inc. All Rights Reserved.

#pragma once

#include "Styling/SlateStyle.h"

class MESHEDITOR_API FMeshEditorStyle
{
public:

	static void Initialize();
	static void Shutdown();
	static TSharedPtr<ISlateStyle> Get() { return StyleSet; }
	static FName GetStyleSetName();

	static const FSlateBrush* GetBrush(FName PropertyName, const ANSICHAR* Specifier = NULL)
	{
		return StyleSet->GetBrush(PropertyName, Specifier);
	}

	static FString InContent( const FString& RelativePath, const ANSICHAR* Extension );


private:

	static TSharedPtr<FSlateStyleSet> StyleSet;
};
