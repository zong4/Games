// 飞机类
class Plane {
    constructor(player, startIndex, homePosition) {
        this.player = player;
        this.startIndex = startIndex; // 启动位置
        this.gridIndex = -1; // 当前在哪个格子上（-1表示在家里）
        this.homePosition = homePosition; // 家的位置
        this.isFinished = false;
        this.isMoving = false;
    }

    draw(ctx, grid, scaleFactor = 1) {
        if (this.isFinished) return;

        let x, y;

        if (this.gridIndex < 0) {
            // 在家里，绘制在棋盘外（需要除以缩放因子）
            x = this.homePosition.x / scaleFactor;
            y = this.homePosition.y / scaleFactor;
        } else {
            // 在棋盘上
            x = grid.col * grids.size + grids.size / 2;
            y = grid.row * grids.size + grids.size / 2;
        }

        const radius = grids.size / 3;

        // 绘制飞机（圆形）
        ctx.fillStyle = this.player.color;
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        ctx.fill();

        // 边框
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 3;
        ctx.stroke();

        // 玩家编号
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 16px Arial';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(this.player.id, x, y);
    }
}

// 玩家类
class Player {
    constructor(id, color, startIndex, homePosition, initialDiceCount = 3, isAI = false) {
        this.id = id;
        this.color = color;
        this.startIndex = startIndex; // 启动位置
        this.plane = new Plane(this, startIndex, homePosition);
        this.initialDiceCount = initialDiceCount; // 初始骰子数量（也是最小保留数量）
        this.maxDiceRolls = initialDiceCount; // 最大骰子次数
        this.currentDiceRolls = initialDiceCount; // 当前剩余骰子次数
        this.isAI = isAI; // 是否为AI玩家
    }

    useDiceRoll(count = 1) {
        this.currentDiceRolls -= count;
        if (this.currentDiceRolls < 0) {
            this.currentDiceRolls = 0;
        }
    }

    needsReturn() {
        return this.currentDiceRolls <= 0;
    }

    returnToHome() {
        this.plane.gridIndex = -1;
        this.currentDiceRolls = this.maxDiceRolls;
    }

    addMaxDiceRolls(count = 1) {
        this.maxDiceRolls += count;
        this.currentDiceRolls += count;
    }

    addEmptyDiceSlot(count = 1) {
        // 只增加最大槽位，不增加当前骰子（空槽位）
        this.maxDiceRolls += count;
    }

    stealDiceRolls(target) {
        // 计算可以偷的数量：目标玩家的空槽位数量
        const availableToSteal = target.maxDiceRolls - target.currentDiceRolls;

        // 计算偷取上限：(目标的最大骰子数 - 起始骰子数) / 2，向上取整
        const stealLimit = Math.ceil((target.maxDiceRolls - target.initialDiceCount) / 2);

        // 实际偷取数量：取两者的最小值
        const stolen = Math.min(availableToSteal, stealLimit);

        if (stolen > 0) {
            // 偷到的是空骰子（只增加maxDiceRolls，不增加currentDiceRolls）
            this.maxDiceRolls += stolen;

            // 被偷的玩家减少最大骰子数，但保留最小数量（初始骰子数量）
            target.maxDiceRolls = Math.max(target.initialDiceCount, target.maxDiceRolls - stolen);
            target.currentDiceRolls = Math.min(target.currentDiceRolls, target.maxDiceRolls);
        }
        return stolen;
    }

    async movePlane(steps, allGrids, board, players, renderCallback) {
        if (this.plane.isFinished || this.plane.isMoving) return { needsReturn: false, events: [] };

        this.plane.isMoving = true;
        const events = [];

        const outlineLength = board.outlineGrids.length;

        // 如果在家里，第一步走到起点位置，剩余步数继续前进
        if (this.plane.gridIndex < 0) {
            // 第一步到达起点
            this.plane.gridIndex = this.startIndex;
            renderCallback();
            await this.sleep(300);
            steps--; // 减去已经走的1步
        }

        // 在棋盘上，逐格移动
        for (let i = 0; i < steps; i++) {
            // 每次循环都重新判断是否在centerpath中
            const isInCenterPath = this.plane.gridIndex >= outlineLength;

            if (isInCenterPath) {
                // 在center path中，正常前进
                const centerPathIndex = this.plane.gridIndex - outlineLength;
                const nextCenterGrid = board.centerGrids[centerPathIndex + 1];

                // 如果下一格不存在，或者不是自己颜色，说明要到终点了
                if (!nextCenterGrid) {
                    // 已经在最后一格，不能再前进
                    this.plane.isFinished = true;
                    break;
                } else if (nextCenterGrid.color !== this.color) {
                    // 下一格不是自己颜色，说明自己的路径已经走完
                    this.plane.isFinished = true;
                    break;
                } else {
                    // 继续前进
                    this.plane.gridIndex++;
                }
            } else {
                // 在outline中，需要检查是否进入center path
                // center path入口是起始格子的前一个格子
                const centerPathEntryIndex = (this.startIndex - 1 + outlineLength) % outlineLength;

                // 检查当前是否在center path入口上
                if (this.plane.gridIndex === centerPathEntryIndex) {
                    // 当前在入口格子上，下一步进入center path
                    const myCenterPathStart = board.centerGrids.findIndex(g => g.color === this.color);
                    if (myCenterPathStart !== -1) {
                        this.plane.gridIndex = outlineLength + myCenterPathStart;
                        events.push(`进入终点路径！`);
                    } else {
                        // 如果找不到，继续在outline上
                        this.plane.gridIndex = (this.plane.gridIndex + 1) % outlineLength;
                    }
                } else {
                    // 继续在outline上绕圈
                    this.plane.gridIndex = (this.plane.gridIndex + 1) % outlineLength;
                }
            }

            renderCallback();
            await this.sleep(300);
        }

        // 移动完成后的检查
        if (!this.plane.isFinished) {
            const currentGrid = allGrids[this.plane.gridIndex];

            // 检查是否追上其他玩家（只在outline上）
            if (this.plane.gridIndex < outlineLength) {
                const caughtPlayer = this.checkCatchOtherPlayer(players);
                if (caughtPlayer) {
                    const stolen = this.stealDiceRolls(caughtPlayer);
                    if (stolen > 0) {
                        events.push(`追上玩家 ${caughtPlayer.id}！偷取 ${stolen} 次骰子`);
                    }
                }

                // 检查同色格子跳跃（只在外圈）- 同色格子就是资源点
                // 但不包括入口格子（起始点前一格）
                const centerPathEntryIndex = (this.startIndex - 1 + outlineLength) % outlineLength;
                const isEntryGrid = this.plane.gridIndex === centerPathEntryIndex;

                if (currentGrid && currentGrid.color === this.color && !isEntryGrid) {
                    // 计算这是第几个同色格子（从起始点开始算）
                    const sameColorIndex = this.calculateSameColorIndex(board);
                    const bonusDice = Math.min(sameColorIndex, 4); // 最多4个

                    this.addEmptyDiceSlot(bonusDice);
                    events.push(`踩到同色格子！获得 +${bonusDice} 空骰子槽位 (最大: ${this.maxDiceRolls})`);
                    const jumped = await this.jumpToNextSameColor(allGrids, board, renderCallback);
                    if (jumped) {
                        events.push(`跳跃完成！`);
                    }
                }
            }
        }

        // 检查是否到达终点（走完自己颜色的centerpath）
        if (this.plane.isFinished) {
            // 已经在移动过程中标记为完成
        } else if (this.plane.gridIndex >= outlineLength) {
            // 在centerpath中，检查是否走完自己颜色的路径
            const centerPathIndex = this.plane.gridIndex - outlineLength;
            const currentCenterGrid = board.centerGrids[centerPathIndex];

            // 找到下一个格子
            const nextCenterGrid = board.centerGrids[centerPathIndex + 1];

            // 如果当前是自己颜色，且下一个不是自己颜色（或没有下一个），说明走完了
            if (currentCenterGrid && currentCenterGrid.color === this.color) {
                if (!nextCenterGrid || nextCenterGrid.color !== this.color) {
                    this.plane.isFinished = true;
                    events.push(`到达终点！`);
                }
            }
        }

        this.plane.isMoving = false;
        return { needsReturn: this.needsReturn(), events };
    }

    checkCatchOtherPlayer(players) {
        for (const player of players) {
            if (player.id !== this.id &&
                !player.plane.isFinished &&
                player.plane.gridIndex === this.plane.gridIndex &&
                player.plane.gridIndex >= 0) {
                return player;
            }
        }
        return null;
    }

    calculateSameColorIndex(board) {
        // 计算当前位置是从起始点开始的第几个同色格子
        const outlineLength = board.outlineGrids.length;
        const currentIndex = this.plane.gridIndex;

        if (currentIndex >= outlineLength) return 1; // 不在outline上，返回1

        let count = 0;
        // 从起始点开始遍历到当前位置
        for (let i = 0; i < outlineLength; i++) {
            const checkIndex = (this.startIndex + i) % outlineLength;
            const grid = board.outlineGrids[checkIndex];

            if (grid && grid.color === this.color) {
                count++;
            }

            // 到达当前位置
            if (checkIndex === currentIndex) {
                break;
            }
        }

        return count;
    }

    async jumpToNextSameColor(allGrids, board, renderCallback) {
        const outlineLength = board.outlineGrids.length;
        const currentIndex = this.plane.gridIndex;

        // 只在外圈查找
        if (currentIndex >= outlineLength) return false;

        // 从当前位置的下一格开始查找下一个同色格子（环形查找）
        for (let offset = 1; offset < outlineLength; offset++) {
            const searchIndex = (currentIndex + offset) % outlineLength;
            const grid = board.outlineGrids[searchIndex];

            if (grid && grid.color === this.color) {
                // 找到下一个同色格子，逐格跳跃过去（使用环形移动）
                for (let j = 0; j < offset; j++) {
                    // 环形移动
                    this.plane.gridIndex = (this.plane.gridIndex + 1) % outlineLength;
                    renderCallback();
                    await this.sleep(200); // 跳跃动画稍快一些
                }

                // 只跳一次，不递归
                return true;
            }
        }

        return false;
    }

    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

// 游戏类
class Game {
    constructor(board) {
        this.board = board;
        this.allGrids = [...board.outlineGrids, ...board.centerGrids];

        // 根据centerPath的颜色决定玩家
        this.players = this.createPlayersFromCenterPaths(board);
        this.currentPlayerIndex = 0;
        this.playersNeedingReturn = new Set(); // 记录需要回家的玩家
        this.gameEnded = false; // 游戏是否已结束

        this.setupUI();
        this.render();
    }

    createPlayersFromCenterPaths(board) {
        const outlineLength = board.outlineGrids.length;
        const players = [];

        // centerGrids按顺序包含了四条路径，找到每条路径的第一个格子
        const centerPathStarts = [];
        let lastColor = null;

        for (const centerGrid of board.centerGrids) {
            // 如果颜色变化，说明是新路径的开始
            if (centerGrid.color !== lastColor) {
                centerPathStarts.push(centerGrid);
                lastColor = centerGrid.color;
            }
        }

        // 为每条centerPath创建一个玩家
        // 在家位置设置在起始点旁边，向棋盘内部偏移
        const homePositions = [];

        centerPathStarts.forEach((centerStart, index) => {
            // centerPath的第一个格子位置
            const pathRow = centerStart.row;
            const pathCol = centerStart.col;

            // 找到外圈中与这个centerPath相邻的入口格子
            let entryRow, entryCol;

            // 判断路径方向，找到入口
            const center = Math.floor(grids.number / 2);
            if (pathRow === center && pathCol < center) {
                // 左边路径，入口在 (center, 0)
                entryRow = center;
                entryCol = 0;
            } else if (pathCol === center - 1 && pathRow < center) {
                // 顶部路径，入口在 (0, center-1)
                entryRow = 0;
                entryCol = center - 1;
            } else if (pathRow === center - 1 && pathCol > center) {
                // 右边路径，入口在 (center-1, grids.number-1)
                entryRow = center - 1;
                entryCol = grids.number - 1;
            } else if (pathCol === center && pathRow > center) {
                // 底部路径，入口在 (grids.number-1, center)
                entryRow = grids.number - 1;
                entryCol = center;
            }

            // 找到外圈中的入口格子
            const entryGrid = board.outlineGrids.find(g =>
                g.row === entryRow && g.col === entryCol
            );

            if (entryGrid) {
                // 获取入口格子的索引，往后1格作为启动位置
                const entryIndex = board.outlineGrids.indexOf(entryGrid);
                const startIndex = (entryIndex + 1) % outlineLength;

                // 获取起始点格子
                const startGrid = board.outlineGrids[startIndex];

                // 根据起始点位置计算在家位置（在起始点旁边向内偏移）
                let homePosition;

                // 判断起始点在棋盘的哪个边
                if (startGrid.col === 0) {
                    // 左边，向右内侧偏移
                    homePosition = {
                        x: startGrid.col * grids.size + grids.size * 1.5,
                        y: startGrid.row * grids.size + grids.size / 2
                    };
                } else if (startGrid.row === 0) {
                    // 上边，向下内侧偏移
                    homePosition = {
                        x: startGrid.col * grids.size + grids.size / 2,
                        y: startGrid.row * grids.size + grids.size * 1.5
                    };
                } else if (startGrid.col === grids.number - 1) {
                    // 右边，向左内侧偏移
                    homePosition = {
                        x: startGrid.col * grids.size - grids.size / 2,
                        y: startGrid.row * grids.size + grids.size / 2
                    };
                } else if (startGrid.row === grids.number - 1) {
                    // 下边，向上内侧偏移
                    homePosition = {
                        x: startGrid.col * grids.size + grids.size / 2,
                        y: startGrid.row * grids.size - grids.size / 2
                    };
                }

                // 使用centerPath的颜色创建玩家
                // 前N个是人类玩家，剩余的是AI玩家
                const isAI = index >= gameSettings.humanPlayerCount;
                const player = new Player(index + 1, centerStart.color, startIndex, homePosition, gameSettings.initialDiceCount, isAI);
                players.push(player);
            }
        });

        // 根据配置只返回指定数量的玩家
        return players.slice(0, gameSettings.playerCount);
    }

    setupUI() {
        // 当前玩家显示
        this.currentPlayerSpan = document.getElementById('currentPlayer');

        // 骰子数量输入
        this.diceCountInput = document.getElementById('diceCount');

        // 投骰子按钮
        this.rollButton = document.getElementById('rollButton');
        this.rollButton.addEventListener('click', () => this.rollDice());

        // 骰子结果显示
        this.diceResultsDiv = document.getElementById('diceResults');

        // 玩家状态显示
        this.playersStatusDiv = document.getElementById('playersStatus');

        // 游戏日志
        this.gameLog = document.getElementById('gameLog');

        this.updateUI();
    }

    updateUI() {
        const currentPlayer = this.players[this.currentPlayerIndex];
        this.currentPlayerSpan.textContent = currentPlayer.isAI ? `${currentPlayer.id} (AI)` : currentPlayer.id;
        this.currentPlayerSpan.style.color = currentPlayer.color;

        // 更新骰子信息
        const diceInfo = document.getElementById('diceInfo');
        if (diceInfo) {
            diceInfo.textContent = `骰子次数: ${currentPlayer.currentDiceRolls}/${currentPlayer.maxDiceRolls}`;
            if (currentPlayer.needsReturn()) {
                diceInfo.style.color = '#f5576c';
                diceInfo.style.fontWeight = 'bold';
            } else {
                diceInfo.style.color = '#333';
                diceInfo.style.fontWeight = 'normal';
            }
        }

        // 如果是AI玩家，禁用控制界面并自动操作
        if (currentPlayer.isAI) {
            this.diceCountInput.disabled = true;
            this.rollButton.disabled = true;
            this.rollButton.textContent = '🤖 AI思考中...';

            // AI自动操作（延迟执行，让玩家看到轮次变化）
            setTimeout(() => this.performAIAction(), 800);
        } else {
            // 更新最大投掷数 - 不能超过当前骰子数量
            this.diceCountInput.max = Math.max(1, currentPlayer.currentDiceRolls);

            // 如果当前输入值超过最大值，重置为最大值
            if (parseInt(this.diceCountInput.value) > this.diceCountInput.max) {
                this.diceCountInput.value = this.diceCountInput.max;
            }

            // 如果没有骰子，禁用输入并修改按钮文本
            if (currentPlayer.needsReturn()) {
                this.diceCountInput.disabled = true;
                this.diceCountInput.value = 0;
                this.rollButton.disabled = false; // 允许点击跳过回合
                this.rollButton.textContent = '⏭️ 跳过回合';
            } else {
                this.diceCountInput.disabled = false;
                this.rollButton.disabled = false; // 启用按钮
                if (parseInt(this.diceCountInput.value) === 0) {
                    this.diceCountInput.value = 1;
                }
                this.rollButton.textContent = '🎲 投掷骰子';
            }
        }

        // 更新所有玩家状态
        this.updatePlayersStatus();
    }

    updatePlayersStatus() {
        if (!this.playersStatusDiv) return;

        this.playersStatusDiv.innerHTML = '';

        this.players.forEach((player, index) => {
            const playerDiv = document.createElement('div');
            playerDiv.className = 'player-item' + (index === this.currentPlayerIndex ? ' active' : '');

            const statusText = player.plane.gridIndex < 0 ? '在家' :
                player.plane.isFinished ? '已完成' :
                    `位置 ${player.plane.gridIndex}`;

            const needsReturn = this.playersNeedingReturn.has(player.id);

            playerDiv.innerHTML = `
                <div class="player-header">
                    <span class="player-name" style="color: ${player.color}">
                        玩家 ${player.id}${player.isAI ? ' 🤖' : ''}
                    </span>
                    <span>${statusText}</span>
                </div>
                <div class="player-details">
                    <div>🎲 骰子: ${player.currentDiceRolls}/${player.maxDiceRolls}</div>
                    ${needsReturn ? '<div style="color: #f5576c;">⚠️ 等待回家</div>' : ''}
                </div>
            `;

            this.playersStatusDiv.appendChild(playerDiv);
        });
    }

    addLog(message, isImportant = false) {
        if (!this.gameLog) return;

        const entry = document.createElement('div');
        entry.className = 'log-entry' + (isImportant ? ' important' : '');
        entry.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
        this.gameLog.appendChild(entry);
        this.gameLog.scrollTop = this.gameLog.scrollHeight;

        // 限制日志数量
        while (this.gameLog.children.length > 50) {
            this.gameLog.removeChild(this.gameLog.firstChild);
        }
    }

    async rollDice() {
        // 检查游戏是否已结束
        if (this.gameEnded) {
            return;
        }

        const currentPlayer = this.players[this.currentPlayerIndex];
        const diceCount = parseInt(this.diceCountInput.value);

        if (diceCount < 1) {
            this.addLog('⚠️ 至少要投掷 1 个骰子', true);
            return;
        }

        // 如果骰子用完，跳过回合
        if (currentPlayer.needsReturn()) {
            this.addLog(`玩家 ${currentPlayer.id} 骰子用完，跳过回合！`, true);
            this.playersNeedingReturn.add(currentPlayer.id);
            await this.nextPlayer();
            this.render();
            return;
        }

        // 检查投掷数量是否超过当前骰子数量
        if (diceCount > currentPlayer.currentDiceRolls) {
            this.addLog(`⚠️ 骰子数量不足！当前只有 ${currentPlayer.currentDiceRolls} 个骰子`, true);
            return;
        }

        this.addLog(`玩家 ${currentPlayer.id} 投掷 ${diceCount} 个骰子`);

        // 投多个骰子
        const results = [];
        for (let i = 0; i < diceCount; i++) {
            results.push(Math.floor(Math.random() * 6) + 1);
        }

        // 消耗骰子次数
        currentPlayer.useDiceRoll(diceCount);
        this.updateUI();

        // 显示结果
        this.showDiceResults(results);
    }

    showDiceResults(results) {
        const currentPlayer = this.players[this.currentPlayerIndex];
        this.diceResultsDiv.innerHTML = '<h3>选择结果:</h3>';

        const grid = document.createElement('div');
        grid.className = 'dice-results-grid';

        results.forEach(result => {
            const button = document.createElement('button');
            button.className = 'dice-btn';
            button.textContent = result;

            // 如果是AI玩家，禁用按钮
            if (currentPlayer.isAI) {
                button.disabled = true;
                button.style.opacity = '0.6';
            } else {
                button.onclick = () => this.selectDiceResult(result);
            }

            grid.appendChild(button);
        });

        this.diceResultsDiv.appendChild(grid);
    }

    async selectDiceResult(steps) {
        const currentPlayer = this.players[this.currentPlayerIndex];

        // 禁用按钮
        this.rollButton.disabled = true;

        this.addLog(`玩家 ${currentPlayer.id} 选择移动 ${steps} 步`);

        // 移动飞机（带动画）
        const result = await currentPlayer.movePlane(steps, this.allGrids, this.board, this.players, () => this.render());

        // 清空骰子结果
        this.diceResultsDiv.innerHTML = '';

        // 显示事件日志
        if (result.events && result.events.length > 0) {
            result.events.forEach(event => {
                this.addLog(`玩家 ${currentPlayer.id}: ${event}`, true);
            });
        }

        // 如果骰子用完，标记需要回家（但不立即回家）
        if (result.needsReturn && currentPlayer.plane.gridIndex >= 0) {
            this.addLog(`玩家 ${currentPlayer.id} 骰子用完！`, true);
            this.playersNeedingReturn.add(currentPlayer.id);
        }

        // 检查是否获胜
        if (currentPlayer.plane.isFinished) {
            this.endGame(currentPlayer);
            return; // 游戏结束，不再继续
        }

        // 切换到下一个玩家（异步处理回家）
        await this.nextPlayer();

        // 重新渲染
        this.render();

        // 启用按钮
        this.rollButton.disabled = false;
    }

    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async performAIAction() {
        const currentPlayer = this.players[this.currentPlayerIndex];

        // 如果游戏已结束，不执行操作
        if (this.gameEnded) {
            return;
        }

        // 如果骰子用完，跳过回合
        if (currentPlayer.needsReturn()) {
            this.addLog(`AI玩家 ${currentPlayer.id} 骰子用完，跳过回合！`, true);
            this.playersNeedingReturn.add(currentPlayer.id);
            await this.nextPlayer();
            this.render();
            return;
        }

        // AI策略：每次只投1个骰子
        const diceCount = 1;
        this.addLog(`AI玩家 ${currentPlayer.id} 投掷 ${diceCount} 个骰子`);

        // 投骰子
        const results = [];
        for (let i = 0; i < diceCount; i++) {
            results.push(Math.floor(Math.random() * 6) + 1);
        }

        this.addLog(`结果: ${results.join(', ')}`);

        // 显示结果
        this.showDiceResults(results);

        // AI自动选择结果（延迟一下让玩家看到结果）
        setTimeout(() => this.aiSelectDiceResult(results[0]), 600);
    }

    async aiSelectDiceResult(result) {
        const currentPlayer = this.players[this.currentPlayerIndex];

        // 如果游戏已结束，不执行操作
        if (this.gameEnded) {
            return;
        }

        // 使用骰子
        currentPlayer.useDiceRoll(1);
        this.addLog(`AI玩家 ${currentPlayer.id} 选择了 ${result}，移动 ${result} 步`);

        // 禁用按钮
        this.rollButton.disabled = true;

        // 移动棋子
        const moveResult = await currentPlayer.movePlane(result, this.allGrids, this.board, this.players, () => this.render());

        // 显示移动事件
        if (moveResult.events.length > 0) {
            moveResult.events.forEach(event => this.addLog(event));
        }

        // 如果骰子用完，标记需要回家（但不立即回家）
        if (moveResult.needsReturn && currentPlayer.plane.gridIndex >= 0) {
            this.addLog(`AI玩家 ${currentPlayer.id} 骰子用完！`, true);
            this.playersNeedingReturn.add(currentPlayer.id);
        }

        // 检查是否获胜
        if (currentPlayer.plane.isFinished) {
            this.endGame(currentPlayer);
            return;
        }

        // 切换到下一个玩家
        await this.nextPlayer();

        // 重新渲染
        this.render();
    }

    endGame(winner) {
        this.gameEnded = true;

        // 禁用所有操作
        this.rollButton.disabled = true;
        this.diceCountInput.disabled = true;

        // 显示获胜信息
        this.addLog('', true);
        this.addLog('🎉🎉🎉 游戏结束！🎉🎉🎉', true);
        this.addLog(`🏆 玩家 ${winner.id} 获胜！🏆`, true);
        this.addLog('', true);

        // 在UI上显示获胜者
        setTimeout(() => {
            const winnerDiv = document.createElement('div');
            winnerDiv.style.cssText = `
                position: fixed;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                color: white;
                padding: 40px 60px;
                border-radius: 20px;
                box-shadow: 0 20px 60px rgba(0,0,0,0.3);
                text-align: center;
                z-index: 10000;
                font-family: Arial, sans-serif;
            `;

            winnerDiv.innerHTML = `
                <h1 style="font-size: 48px; margin: 0 0 20px 0;">🎉 游戏结束 🎉</h1>
                <div style="font-size: 72px; margin: 20px 0;">🏆</div>
                <h2 style="font-size: 36px; margin: 0; color: ${winner.color}; text-shadow: 2px 2px 4px rgba(0,0,0,0.3);">
                    玩家 ${winner.id} 获胜！
                </h2>
                <button onclick="location.reload()" style="
                    margin-top: 30px;
                    padding: 15px 40px;
                    font-size: 20px;
                    background: white;
                    color: #667eea;
                    border: none;
                    border-radius: 10px;
                    cursor: pointer;
                    font-weight: bold;
                    box-shadow: 0 4px 15px rgba(0,0,0,0.2);
                ">🔄 重新开始</button>
            `;

            document.body.appendChild(winnerDiv);
        }, 500);
    }

    async nextPlayer() {
        this.currentPlayerIndex = (this.currentPlayerIndex + 1) % this.players.length;

        // 如果回到第一个玩家（一轮结束），统一处理回家和补给
        if (this.currentPlayerIndex === 0) {
            await this.processReturnToHome();
        }

        this.updateUI();
    }

    async processReturnToHome() {
        let hasActivity = false; // 标记是否有需要处理的事件

        // 处理需要回家的玩家
        if (this.playersNeedingReturn.size > 0) {
            if (!hasActivity) {
                this.addLog('=== 一轮结束，处理补给 ===', true);
                await this.sleep(500);
                hasActivity = true;
            }

            const playerIds = Array.from(this.playersNeedingReturn);
            for (const playerId of playerIds) {
                const player = this.players.find(p => p.id === playerId);
                if (player && player.plane.gridIndex >= 0) {
                    this.addLog(`玩家 ${player.id} 返回家补给！`, true);
                    await this.sleep(500);
                    player.returnToHome();
                    this.render();
                    await this.sleep(500);
                }
            }
            this.playersNeedingReturn.clear();
        }

        // 为所有在家的玩家补充骰子
        for (const player of this.players) {
            if (player.plane.gridIndex < 0 && player.currentDiceRolls < player.maxDiceRolls) {
                if (!hasActivity) {
                    this.addLog('=== 一轮结束，处理补给 ===', true);
                    await this.sleep(500);
                    hasActivity = true;
                }

                const restored = player.maxDiceRolls - player.currentDiceRolls;
                player.currentDiceRolls = player.maxDiceRolls;
                this.addLog(`玩家 ${player.id} 在家补充 ${restored} 个骰子`, true);
            }
        }

        if (hasActivity) {
            this.addLog('=== 补给完成，新一轮开始 ===', true);
            await this.sleep(500);
        }
    }

    render() {
        // 重绘棋盘
        this.board.draw();

        // 绘制所有飞机（不绘制在家的）
        this.board.ctx.save();
        this.board.ctx.scale(this.board.scaleFactor, this.board.scaleFactor);
        this.players.forEach(player => {
            if (!player.plane.isFinished && player.plane.gridIndex >= 0) {
                // 只绘制在棋盘上的飞机
                const grid = this.allGrids[player.plane.gridIndex];
                if (grid) {
                    player.plane.draw(this.board.ctx, grid, this.board.scaleFactor);
                }
            }
        });
        this.board.ctx.restore();
    }
}

// 初始化游戏
let currentGame = null;

function startGame() {
    // 读取配置
    const playerCountInput = document.getElementById('playerCountInput');
    const humanPlayerCountInput = document.getElementById('humanPlayerCountInput');
    const initialDiceInput = document.getElementById('initialDiceInput');
    const gridNumberInput = document.getElementById('gridNumberInput');

    gameSettings.playerCount = parseInt(playerCountInput.value);
    gameSettings.humanPlayerCount = parseInt(humanPlayerCountInput.value);
    gameSettings.initialDiceCount = parseInt(initialDiceInput.value);
    gameSettings.gridNumber = parseInt(gridNumberInput.value);

    // 验证输入
    if (gameSettings.playerCount < 2 || gameSettings.playerCount > 4) {
        alert('总玩家数量必须在 2-4 之间！');
        return;
    }

    if (gameSettings.humanPlayerCount < 0 || gameSettings.humanPlayerCount > 4) {
        alert('人类玩家数量必须在 0-4 之间！');
        return;
    }

    if (gameSettings.humanPlayerCount > gameSettings.playerCount) {
        alert('人类玩家数量不能超过总玩家数量！');
        return;
    }

    if (gameSettings.initialDiceCount < 1 || gameSettings.initialDiceCount > 10) {
        alert('初始骰子数量必须在 1-10 之间！');
        return;
    }

    if (gameSettings.gridNumber < 8 || gameSettings.gridNumber > 20) {
        alert('棋盘格子数量必须在 8-20 之间！');
        return;
    }

    if (gameSettings.gridNumber % 4 !== 0) {
        alert('棋盘格子数量必须是4的倍数！');
        return;
    }

    // 应用棋盘格子数量配置
    grids.number = gameSettings.gridNumber;

    // 隐藏配置面板，显示游戏
    document.getElementById('configPanel').style.display = 'none';
    document.querySelector('.game-container').style.display = 'flex';

    // 创建游戏
    const board = new Board('board');
    currentGame = new Game(board);
}

window.addEventListener('DOMContentLoaded', () => {
    // 绑定开始游戏按钮
    document.getElementById('startGameButton').addEventListener('click', startGame);
});

