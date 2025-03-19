@echo off

glslangValidator -V -S vert draw.vert.txt -o draw.vert.spv

glslangValidator -V -S frag draw.frag.txt -o draw.frag.spv

glslangValidator -V -S vert out24.vert.txt -o out24.vert.spv

glslangValidator -V -S frag out24.frag.txt -o out24.frag.spv

glslangValidator -V -S vert out16.vert.txt -o out16.vert.spv

glslangValidator -V -S frag out16.frag.txt -o out16.frag.spv

glslangValidator -V -S vert resetdepth.vert.txt -o resetdepth.vert.spv

glslangValidator -V -S frag resetdepth.frag.txt -o resetdepth.frag.spv

glslangValidator -V -S vert display.vert.txt -o display.vert.spv

glslangValidator -V -S frag display.frag.txt -o display.frag.spv

pause