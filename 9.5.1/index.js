// Возвращает массив уникальных значений
function uniqueArray(arr) {
    return [...new Set(arr)];
}

// Сортирует массив по убыванию
function sortDescending(arr) {
    return arr.slice().sort((a, b) => b - a);
}

// Находит среднее значение массива
function averageArray(arr) {
    if (!arr.length) return 0;
    return arr.reduce((sum, num) => sum + num, 0) / arr.length;
}

module.exports = {
    uniqueArray,
    sortDescending,
    averageArray
};
