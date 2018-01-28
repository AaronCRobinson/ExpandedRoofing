@echo off
SET "ProjectName=ExpandedRoofing"
SET "SolutionDir=C:\Users\robin\Desktop\Games\RimWorld Modding\Source\ExpandedRoofing\Source"
@echo on

del /S /Q "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Defs\*"

xcopy /S /Y "%SolutionDir%\..\About\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\About\"
xcopy /S /Y "%SolutionDir%\..\Assemblies\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Assemblies\"
xcopy /S /Y "%SolutionDir%\..\Defs\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Defs\"
xcopy /S /Y "%SolutionDir%\..\Patches\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Patches\"
xcopy /S /Y "%SolutionDir%\..\Textures\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Textures\"
xcopy /S /Y "%SolutionDir%\..\Languages\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Languages\"