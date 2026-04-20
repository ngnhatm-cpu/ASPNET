document.addEventListener("DOMContentLoaded", () => {
    initNavbar();
    if (document.getElementById("mangaGrid")) {
        checkUrlParams();
        loadMangas();
        loadCategories();
        initSearch();
    } else {
        // Nếu ở các trang khác (detail, ranking), vẫn khởi kiến search để redirect
        initSearch();
    }
});

function checkUrlParams() {
    const params = new URLSearchParams(window.location.search);
    const search = params.get("search");
    if (search) {
        // Đặt giá trị cho input search nếu có
        const searchInput = document.getElementById("searchInput");
        if (searchInput) searchInput.value = search;
    }
}

let searchTimeout = null;
function initSearch() {
    const searchInput = document.getElementById("searchInput");
    if (!searchInput) return;

    searchInput.addEventListener("input", (e) => {
        const keyword = e.target.value.trim();
        
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            if (window.location.pathname.includes("index.html") || window.location.pathname === "/") {
                if (keyword.length > 0) {
                    performSearch(keyword);
                } else {
                    loadMangas();
                }
            } else {
                // Nếu không ở trang chủ, redirect về trang chủ kèm search param
                if (keyword.length > 0) {
                    window.location.href = `index.html?search=${encodeURIComponent(keyword)}`;
                }
            }
        }, 500);
    });
}

async function performSearch(keyword) {
    const grid = document.getElementById("mangaGrid");
    try {
        const response = await fetch(`/api/Mangas/search?keyword=${encodeURIComponent(keyword)}`);
        if (!response.ok) throw new Error("Search failed");
        
        const results = await response.json();
        renderMangas(results);
    } catch (error) {
        console.error("Search error", error);
        grid.innerHTML = `<p class="col-span-full text-center text-red-500 font-bold uppercase tracking-widest text-[10px]">Lỗi tìm kiếm: ${error.message}</p>`;
    }
}

function initNavbar() {
    const navAuthArea = document.getElementById("navAuthArea");
    if (!navAuthArea) return;

    const token = localStorage.getItem("token");
    const userJson = localStorage.getItem("user");

    if (token && userJson) {
        const user = JSON.parse(userJson);
        const avatarLetter = user.username ? user.username.charAt(0).toUpperCase() : "U";
        
        navAuthArea.innerHTML = `
            <div class="flex items-center gap-6">
                <!-- Ví Xu -->
                <div id="btnOpenTopup" class="flex items-center gap-2 bg-white/5 border border-white/10 px-3 py-1.5 rounded-full cursor-pointer hover:bg-white/10 transition-colors">
                    <span class="material-symbols-outlined text-secondary-fixed text-sm">monetization_on</span>
                    <span class="text-secondary-fixed font-bold text-sm tracking-widest">${user.balance || 0} Xu</span>
                </div>
                <!-- Bookmark -->
                <button class="text-white/40 hover:text-white transition-colors">
                    <span class="material-symbols-outlined">bookmarks</span>
                </button>
                <!-- Avatar & Menu -->
                <div class="flex items-center gap-3 ml-2 cursor-pointer group relative">
                    <div class="text-right hidden md:block">
                        <p class="text-xs font-bold text-white uppercase tracking-wider">${user.username}</p>
                    </div>
                    <div class="w-10 h-10 rounded-full bg-gradient-to-br from-primary to-primary-container flex items-center justify-center font-black text-white shadow-lg">
                        ${avatarLetter}
                    </div>
                    <!-- Dropdown giả -->
                    <div class="absolute top-full right-0 mt-4 w-48 bg-[#1f1f25] border border-white/10 rounded-lg shadow-2xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all flex flex-col py-2">
                        <a href="history.html" class="text-left px-4 py-2 text-[10px] text-white/60 hover:bg-white/5 font-bold uppercase tracking-widest transition-colors flex items-center gap-2">
                            <span class="material-symbols-outlined text-sm">history</span>
                            Lịch sử giao dịch
                        </a>
                        <button onclick="logout()" class="text-left px-4 py-2 text-[10px] text-red-400 hover:bg-white/5 font-bold uppercase tracking-widest transition-colors flex items-center gap-2">
                            <span class="material-symbols-outlined text-sm">logout</span>
                            Đăng Xuất
                        </button>
                    </div>
                </div>
            </div>
        `;
    } else {
        navAuthArea.innerHTML = `
            <a href="login.html" class="text-white font-bold uppercase text-xs tracking-widest hover:text-primary transition-colors">Đăng Nhập</a>
            <a href="register.html" class="bg-primary hover:bg-white text-on-primary-container font-black py-2.5 px-6 rounded uppercase tracking-widest text-xs transition-all shadow-lg hover:scale-105">Đăng Ký</a>
        `;
    }
}

function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    window.location.reload();
}

async function loadMangas() {
    const grid = document.getElementById("mangaGrid");
    const params = new URLSearchParams(window.location.search);
    const searchParam = params.get("search");

    if (searchParam) {
        return performSearch(searchParam);
    }

    try {
        const response = await fetch("/api/Mangas");
        if (!response.ok) throw new Error("Failed to load");
        
        const mangas = await response.json();
        
        // Cập nhật Hero Stack với 3 truyện mới nhất
        const top3 = mangas.slice(0, 3);
        updateHeroStack(top3);

        renderMangas(mangas);
    } catch (error) {
        console.error("Error loading mangas", error);
        grid.innerHTML = `<p class="col-span-full text-center text-red-500 font-bold">Lỗi tải dữ liệu truyện: ${error.message}</p>`;
    }
}

async function loadCategories() {
    const filterContainer = document.getElementById("genreFilters");
    if (!filterContainer) return;

    try {
        const response = await fetch("/api/Categories");
        const categories = await response.json();
        
        // Render pills
        const existingAllPill = filterContainer.innerHTML;
        filterContainer.innerHTML = existingAllPill + categories.map(cat => `
            <button onclick="filterByGenre(${cat.id}, this)" 
                    class="genre-pill px-6 py-2.5 rounded-full border border-white/5 bg-white/5 text-white/40 text-[10px] font-black uppercase tracking-widest transition-all hover:border-primary/50 hover:text-white">
                ${cat.name}
            </button>
        `).join('');
    } catch (error) {
        console.error("Error loading categories", error);
    }
}

async function filterByGenre(categoryId, btn) {
    const grid = document.getElementById("mangaGrid");
    
    // UI Update pills
    document.querySelectorAll(".genre-pill").forEach(p => {
        p.classList.remove("border-primary", "bg-primary/10", "text-primary");
        p.classList.add("border-white/5", "bg-white/5", "text-white/40");
    });
    if (btn) {
        btn.classList.add("border-primary", "bg-primary/10", "text-primary");
        btn.classList.remove("border-white/5", "bg-white/5", "text-white/40");
    }

    if (categoryId === null) {
        return loadMangas();
    }

    try {
        const response = await fetch(`/api/Mangas/category/${categoryId}`);
        const mangas = await response.json();
        renderMangas(mangas);
        
        // Scroll to grid
        grid.scrollIntoView({ behavior: 'smooth', block: 'start' });
    } catch (error) {
        console.error("Filter error", error);
    }
}

function renderMangas(mangas) {
    const grid = document.getElementById("mangaGrid");
    grid.innerHTML = "";

    if (!mangas || mangas.length === 0) {
        grid.innerHTML = `<p class="col-span-full text-center text-white/40">Chưa có truyện nào.</p>`;
        return;
    }

    mangas.forEach(manga => {
        // Use fallback logic for images 
        const coverHtml = manga.coverImageUrl 
            ? `<img class="w-full h-full object-cover" src="${manga.coverImageUrl}" alt="${manga.title}">`
            : `<div class="w-full h-full flex flex-col items-center justify-center bg-surface-container-high text-white/20"><span class="material-symbols-outlined text-4xl mb-2">image_not_supported</span><span class="text-xs font-bold font-headline uppercase">NO IMG</span></div>`;

        const card = document.createElement("div");
        card.className = "manga-card group cursor-pointer";
        card.innerHTML = `
            <a href="detail.html?id=${manga.id}" class="block">
                <div class="aspect-[2/3] rounded-xl overflow-hidden mb-3 relative bg-surface-container border border-white/5 shadow-lg relative">
                    ${coverHtml}
                    <!-- Overlay Overlay -->
                    <div class="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity"></div>
                    <!-- Hover Action -->
                    <div class="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                        <span class="px-4 py-2 bg-primary text-on-primary-container font-black text-[10px] uppercase tracking-widest rounded-full shadow-xl">Xem chi tiết</span>
                    </div>
                    <!-- Badge -->
                    ${manga.categoryName ? `<div onclick="event.preventDefault(); filterByGenre(${manga.categoryId})" class="absolute top-2 left-2 bg-[#0A0A0F]/80 backdrop-blur-sm text-white px-2 py-1 rounded text-[8px] uppercase tracking-widest font-bold border border-white/10 hover:bg-primary transition-colors hover:text-on-primary-container z-30">${manga.categoryName}</div>` : ''}
                </div>
                <h3 class="font-black text-white font-headline text-sm line-clamp-1 mb-1 group-hover:text-primary transition-colors">${manga.title}</h3>
                <p class="text-[10px] text-white/40 uppercase tracking-widest font-bold line-clamp-1">${manga.author || "Đang cập nhật"}</p>
            </a>
        `;
        grid.appendChild(card);
    });
}

function updateHeroStack(top3) {
    const stackContainer = document.getElementById("heroFloatingStack");
    if (!stackContainer) return;

    if (!top3 || top3.length === 0) {
        stackContainer.style.display = "none";
        return;
    }

    const defaultImg = "https://via.placeholder.com/300x450?text=Premium+Manga";
    
    // Tạo 3 bìa truyện với góc xoay khác nhau như mẫu
    const rotations = ["rotate-[-12deg] z-10", "z-20", "rotate-[12deg] z-10"];
    
    stackContainer.innerHTML = top3.map((m, i) => {
        const imgSrc = m.coverImageUrl || defaultImg;
        const rotClass = rotations[i] || "";
        return `
            <div class="w-48 md:w-64 aspect-[2/3] block select-none pointer-events-none ${rotClass}">
                <img class="w-full h-full object-cover rounded-xl shadow-2xl border border-white/10" 
                     src="${imgSrc}" alt="${m.title}">
            </div>
        `;
    }).join('');
}

// =============================================
// TOPUP MODAL LOGIC
// =============================================

function openTopupModal() {
    const modal = document.getElementById("topupModal");
    const content = document.getElementById("topupContent");
    if (!modal) return;

    modal.classList.remove("hidden");
    modal.classList.add("flex");
    setTimeout(() => {
        modal.classList.remove("opacity-0");
        content?.classList.remove("scale-95");
    }, 10);
}

function closeTopupModal() {
    const modal = document.getElementById("topupModal");
    const content = document.getElementById("topupContent");
    if (!modal) return;

    modal.classList.add("opacity-0");
    content?.classList.add("scale-95");
    setTimeout(() => {
        modal.classList.add("hidden");
        modal.classList.remove("flex");
        // Reset loading state
        document.getElementById("topupLoading")?.classList.add("hidden");
        const btns = document.querySelectorAll(".package-btn");
        btns.forEach(b => b.disabled = false);
    }, 300);
}

async function selectPackage(amount, xu) {
    const loading = document.getElementById("topupLoading");
    const btns = document.querySelectorAll(".package-btn");
    
    try {
        loading.classList.remove("hidden");
        btns.forEach(b => b.disabled = true);

        const response = await apiFetch("/Payments/create-topup", {
            method: "POST",
            body: JSON.stringify(amount)
        });

        if (response.paymentUrl) {
            window.location.href = response.paymentUrl;
        } else {
            throw new Error("Không nhận được link thanh toán.");
        }
    } catch (error) {
        alert("Lỗi: " + error.message);
        loading.classList.add("hidden");
        btns.forEach(b => b.disabled = false);
    }
}

// Lắng nghe sự kiện click nạp xu từ Navbar (vì Navbar được render động)
document.addEventListener("click", (e) => {
    if (e.target.closest("#btnOpenTopup")) {
        openTopupModal();
    }
});
