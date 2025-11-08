class Grid {
    constructor(row, col, cnt) {
        this.row = row;
        this.col = col;
        this.color = cnt % 4 == 0 ? colors.red : cnt % 4 == 1 ? colors.blue : cnt % 4 == 2 ? colors.green : colors.yellow;
    }

    draw(ctx) {
        ctx.fillStyle = this.color;
        ctx.fillRect(this.col * grids.size, this.row * grids.size, grids.size, grids.size);

        // Outline the cell with border color
        ctx.strokeStyle = colors.border;
        ctx.lineWidth = 1;
        ctx.strokeRect(this.col * grids.size, this.row * grids.size, grids.size, grids.size);
    }
}


class Board {
    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas.getContext('2d');
        // 使用较小的尺寸以适应新布局
        const scaleFactor = 0.8; // 缩小到80%
        this.canvas.width = grids.size * grids.number * scaleFactor;
        this.canvas.height = grids.size * grids.number * scaleFactor;
        this.scaleFactor = scaleFactor;

        this.outlineGrids = this.createOutlineGrids();
        this.centerGrids = this.createCenterGrids();
        this.draw();
    }

    createOutlineGrids() {
        const outlineGrids = [];
        let cnt = 0;

        // row: 0→gridSize-1, col: 0
        for (let row = 0; row < grids.number; row++) {
            outlineGrids.push(new Grid(row, 0, cnt++));
        }
        // row: gridSize-1, col: 1→gridSize-1   
        for (let col = 1; col < grids.number; col++) {
            outlineGrids.push(new Grid(grids.number - 1, col, cnt++));
        }
        // row: gridSize-2→0, col: gridSize-1
        for (let row = grids.number - 2; row >= 0; row--) {
            outlineGrids.push(new Grid(row, grids.number - 1, cnt++));
        }
        // row: 0, col: gridSize-2→1
        for (let col = grids.number - 2; col >= 1; col--) {
            outlineGrids.push(new Grid(0, col, cnt++));
        }

        return outlineGrids;
    }

    createCenterGrids() {
        const centerGrids = [];

        // 查找外圈对应位置的颜色
        const findOutlineColor = (row, col) => {
            const grid = this.outlineGrids.find(g => g.row === row && g.col === col);
            return grid ? grid.color : colors.lightGray;
        };

        {
            const center = Math.floor(grids.number / 2);
            const leftCenterColor = findOutlineColor(center, 0);
            const leftColorIndex = Object.values(colors).indexOf(leftCenterColor);
            for (let col = 1; col < center; col++) {
                centerGrids.push(new Grid(center, col, leftColorIndex));
            }
        }

        {
            const center = Math.floor(grids.number / 2) - 1;
            const topCenterColor = findOutlineColor(0, center);
            const topColorIndex = Object.values(colors).indexOf(topCenterColor);
            for (let row = 1; row < center + 1; row++) {
                centerGrids.push(new Grid(row, center, topColorIndex));
            }
        }

        {
            const center = Math.floor(grids.number / 2) - 1;
            const rightCenterColor = findOutlineColor(center, grids.number - 1);
            const rightColorIndex = Object.values(colors).indexOf(rightCenterColor);
            for (let col = grids.number - 2; col > center; col--) {
                centerGrids.push(new Grid(center, col, rightColorIndex));
            }
        }

        {
            const center = Math.floor(grids.number / 2);
            const bottomCenterColor = findOutlineColor(grids.number - 1, center);
            const bottomColorIndex = Object.values(colors).indexOf(bottomCenterColor);
            for (let row = grids.number - 2; row > center - 1; row--) {
                centerGrids.push(new Grid(row, center, bottomColorIndex));
            }
        }

        return centerGrids;
    }

    draw() {
        this.ctx.fillStyle = colors.lightGray;
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

        this.ctx.save();
        this.ctx.scale(this.scaleFactor, this.scaleFactor);

        // Outer ring
        for (const grid of this.outlineGrids) {
            grid.draw(this.ctx);
        }

        // Path to center
        for (const grid of this.centerGrids) {
            grid.draw(this.ctx);
        }

        this.ctx.restore();
    }
}
