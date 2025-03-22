@echo off

del *.spv

pause

glslangValidator -V -S vert draw.vert.txt -o draw.vert.spv

glslangValidator -V -S frag draw.frag.txt -o draw.frag.spv

glslangValidator -V -S vert out24.vert.txt -o out24.vert.spv

glslangValidator -V -S frag out24.frag.txt -o out24.frag.spv

glslangValidator -V -S vert out16.vert.txt -o out16.vert.spv

glslangValidator -V -S frag out16.frag.txt -o out16.frag.spv

pause