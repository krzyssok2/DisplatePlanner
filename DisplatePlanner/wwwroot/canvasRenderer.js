window.canvasRenderer = {
    imageCache: {},

    render: function (canvasId, renderData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Canvas not found:', canvasId);
            return;
        }
        const ctx = canvas.getContext('2d');

        // Clear canvas
        ctx.clearRect(0, 0, renderData.canvasWidth, renderData.canvasHeight);

        // Enable image smoothing once
        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = 'high';

        // Create a Set of selected plate IDs for quick lookup
        const selectedIds = new Set(
            renderData.selectedPlates?.map(p => p.id) || []
        );

        // Separate plates into selected and non-selected
        const nonSelectedPlates = [];
        const selectedPlateData = [];

        renderData.plates.forEach(plate => {
            if (selectedIds.has(plate.id)) {
                selectedPlateData.push(plate);
            } else {
                nonSelectedPlates.push(plate);
            }
        });

        // Draw non-selected plates first
        nonSelectedPlates.forEach(plate => {
            let img = this.imageCache[plate.imageUrl];
            if (!img) {
                img = document.getElementById(plate.imageUrl);
                if (img) this.imageCache[plate.imageUrl] = img;
            }
            if (img) {
                ctx.drawImage(img, plate.x, plate.y, plate.width, plate.height);
            }
        });

        // Draw selected plates on top WITH their outlines immediately after each plate
        selectedPlateData.forEach(plate => {
            // Draw the plate image
            let img = this.imageCache[plate.imageUrl];
            if (!img) {
                img = document.getElementById(plate.imageUrl);
                if (img) this.imageCache[plate.imageUrl] = img;
            }
            if (img) {
                ctx.drawImage(img, plate.x, plate.y, plate.width, plate.height);
            }

            // Draw the red outline INSIDE the plate boundaries
            ctx.save();
            ctx.strokeStyle = 'red';
            ctx.lineWidth = 1.5 / renderData.zoomLevel;

            // Inset by half the line width to keep it inside
            const inset = (1.5 / renderData.zoomLevel) / 2;
            ctx.strokeRect(
                plate.x + inset,
                plate.y + inset,
                plate.width - (inset * 2),
                plate.height - (inset * 2)
            );
            ctx.restore();
        });

        // Draw selection box
        if (renderData.state === 'Selecting' && renderData.selectionBox) {
            ctx.save();
            ctx.setLineDash([
                6 / renderData.zoomLevel,
                2 / renderData.zoomLevel
            ]);
            ctx.strokeStyle = 'black';
            ctx.lineWidth = 1 / renderData.zoomLevel;
            ctx.fillStyle = 'rgba(0, 0, 255, 0.2)';

            ctx.beginPath();
            ctx.rect(
                renderData.selectionBox.x,
                renderData.selectionBox.y,
                renderData.selectionBox.width,
                renderData.selectionBox.height
            );
            ctx.fill();
            ctx.stroke();
            ctx.restore();
        }

        // Draw alignment lines
        if (renderData.state === 'Dragging' && renderData.alignmentLines && renderData.alignmentLines.length > 0) {
            ctx.save();
            ctx.strokeStyle = 'blue';
            ctx.lineWidth = 1 / renderData.zoomLevel;
            ctx.beginPath();

            renderData.alignmentLines.forEach(line => {
                ctx.moveTo(line.x, line.y);
                if (line.isVertical) {
                    ctx.lineTo(line.x, line.y + line.length);
                } else {
                    ctx.lineTo(line.x + line.length, line.y);
                }
            });

            ctx.stroke();
            ctx.restore();
        }
    },

    setZoom: function (canvasId, zoomLevel) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        ctx.setTransform(zoomLevel, 0, 0, zoomLevel, 0, 0);
    },

    clearCache: function () {
        this.imageCache = {};
    }
};