#!/bin/bash

# --- SETTINGS ---
# Increase DEPTH (e.g., 250) for more pop, decrease (e.g., 50) for flatter looks
DEPTH=200
# Change to "true" if you want a slight blur for smoother bevels
SMOOTH=false

# Create an output directory to keep things clean
mkdir -p Normals

for img in *.png; do
    # Skip files that are already normal maps or meta files
    if [[ "$img" == *"_normal.png" ]] || [[ "$img" == *".meta" ]]; then
        continue
    fi

    echo "Processing $img..."

    # Base command
    CMD="convert \"$img\" -colorspace gray"

    # Optional: Add slight blur for smoother pixel transitions
    if [ "$SMOOTH" = true ]; then
        CMD="$CMD -blur 0x0.5"
    fi

    # Generate the slopes using Sobel and encode to Tangent Space (RGB)
    # 1. Morphology calculates X/Y derivatives
    # 2. -fx scales values to the 0.5 neutral point (128, 128, 255 range)
    # 3. -negate fixes the Green channel for Unity/OpenGL standards
    eval $CMD -morphology Convolve Sobel:$DEPTH \
        -channel RG -fx \"u.r/2+0.5, u.g/2+0.5, 0.5\" \
        -separate -swap 0,1 -combine \
        -channel G -negate +channel \
        "Normals/${img%.png}_normal.png"

done

echo "Done! Check the 'Normals' folder."
