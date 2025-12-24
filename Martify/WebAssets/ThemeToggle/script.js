const checkbox = document.getElementById('themeSwitch');

// 1. Hàm này để C# gọi xuống cập nhật trạng thái hiển thị
function setSwitchState(isDark) {
    checkbox.checked = isDark;
}

// 2. Gửi sự kiện lên C# khi người dùng click
checkbox.addEventListener('change', () => {
    const message = checkbox.checked ? 'DarkMode' : 'LightMode';
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(message);
    }
});