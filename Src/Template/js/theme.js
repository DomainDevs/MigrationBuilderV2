const button = document.getElementById("themeButton");
const icon = document.getElementById("themeIcon");
const theme = document.getElementById("theme");

const themes = [
    {
        name: "light",
        icon: "moon-star"
    },
    {
        name: "dark",
        icon: "sun-medium"
    },
    {
        name: "neon",
        icon: "sparkles"
    },
    {
        name: "coral",
        icon: "flame"
    }
];

initializeTheme();

button?.addEventListener("click", toggleTheme);

function initializeTheme() {
    const savedName = localStorage.getItem("theme");

    const currentIndex = themes.findIndex(
        x => x.name === savedName
    );

    applyTheme(
        currentIndex >= 0
            ? themes[currentIndex]
            : themes[0]
    );
}

function toggleTheme() {
    const currentName = localStorage.getItem("theme");

    const currentIndex = themes.findIndex(
        x => x.name === currentName
    );

    const nextIndex =
        currentIndex >= 0
            ? (currentIndex + 1) % themes.length
            : 0;

    applyTheme(themes[nextIndex]);
}

function applyTheme(themeConfig) {
    theme.href = `css/theme-${themeConfig.name}.css`;

    localStorage.setItem(
        "theme",
        themeConfig.name
    );

    if (icon) {
        icon.setAttribute(
            "data-lucide",
            themeConfig.icon
        );

        lucide.createIcons();
    }
}