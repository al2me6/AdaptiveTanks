:: DDS Encoder batch file
::
:: Uses CineboxAndrew's QuickTex encoder to convert PNGs to appropriate DDS files
:: (BC3, non-DX10) for use in KSP. Batch written by Kavaeric
::
:: Install the latest version of Python first
:: https://www.python.org/downloads/windows/
:: Make sure to install for all users and add it to the system PATH.
::
:: Then install https://pypi.org/project/quicktex/
::
:: Then, plop this batch file in the folder with your PNG textures, and it'll
:: convert PNGs to DDSes, using the suffix for each .PNG file to determine
:: the type of texture:
::
:: - EngineTexture_C.png -> diffuse/specular texture
:: - EngineTexture_N.png -> normal texture
:: - EngineTexture_E.png -> emissive texture
::
:: The converter will automatically flip the image vertically.
:: Feel free to reconfigure the wildcards or suffixes below as you see fit.

:: Colour/diffuse map
quicktex encode bc3 -N *_C.png

:: Normal map
quicktex encode bc3 -n *_N.png

:: Emission map
quicktex encode bc3 -N *_E.png

PAUSE