@echo off

del *.spv

pause

glslangValidator -V -S vert draw.vert.txt -o draw.vert.spv

glslangValidator -V -S frag draw.frag.txt -o draw.frag.spv

glslangValidator -V -S vert out.vert.txt -o out.vert.spv

glslangValidator -V -S frag out.frag.txt -o out.frag.spv


pause