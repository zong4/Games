// Colors
const colors = {
    red: '#f5576c',
    blue: '#57a7f5',
    green: '#57f5a7',
    yellow: '#f5d157',
    lightGray: '#ecf0f1',
    border: '#34495e'
};

// Grids
const grids = {
    number: 12,  // 默认值，会被 gameSettings 覆盖
    size: 50,
};

// Game settings (can be modified by UI)
const gameSettings = {
    playerCount: 4,           // 总玩家数量 (2-4)
    humanPlayerCount: 1,      // 人类玩家数量 (0-4)，剩余自动为AI
    initialDiceCount: 3,      // 初始骰子数量（也是最小保留数量）
    gridNumber: 12,           // 棋盘格子数量（1/4圈的格子数）
};