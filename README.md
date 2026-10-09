# SenseGlove R1 Unity Wrapper

This is the public repository used for the R1 <> Unity Integration.


# Requirements
- Unity version 6.3 or higher
- A Python environment that can run the R1 Python API: https://github.com/Adjuvo/SenseGlove-R1-API
- Semi-optionl: SenseGlove Unity Plugin: https://github.com/Adjuvo/SenseGlove-Unity - for 3d Hand Model Animations and a grab/release system.
- Optional: Package Manager > Add By Name > “com.unity.nuget.newtonsoft-json” when running Unit Tests -> Available by un-commenting the #R1_UNIT_TEST_ENABLED define in R1_UnitTestData.cs.


# Getting Started
- Run any of the example scripts in your R1 Python API workspace.
- Run the R1/Demo/R1_BasicDemo scene included in the package
- Wait for your gloves to show up inside the demo


# Package Structure
For now, this version requires you to use the SenseGlove Unity Plugin alongside it to integrate with the 3D Hand Model animations and grab / release system. If all you are after is an R1 API inside, you can also forgo this implementation. 

It's possible to forgo this unitypackage by not importing or removing the R1_HapticGlove script.


# ToDo:
- Linux Support
- Refactor this API into a proper UPM format so it can be imported directly from git through the Package manager Window (Requires us to potentially do the same to the SG Unity Package).
- AFFB integration into a VR application?
- Using Robot Hand Mapper (RHM) output instead of the 'percentage bent' approach.


