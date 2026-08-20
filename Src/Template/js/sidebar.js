const menuBtn = document.getElementById('menuButton');
const sidebar = document.querySelector('.sidebar');

menuBtn?.addEventListener('click', () => {
    const isOpen = sidebar.classList.toggle('open');
    menuBtn.setAttribute('aria-expanded', isOpen);
});