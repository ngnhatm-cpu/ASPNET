document.addEventListener("DOMContentLoaded", () => {
    loadRankings();
});

async function loadRankings() {
    const listContainer = document.getElementById("rankingList");
    try {
        const response = await fetch("/api/Mangas/ranking");
        if (!response.ok) throw new Error("Failed to load rankings");

        const mangas = await response.json();
        renderRankings(mangas);
    } catch (error) {
        console.error("Error loading rankings", error);
        listContainer.innerHTML = `<p class="text-center text-red-500 font-bold uppercase tracking-widest text-xs">Không thể tải bảng xếp hạng: ${error.message}</p>`;
    }
}

function renderRankings(mangas) {
    const listContainer = document.getElementById("rankingList");
    listContainer.innerHTML = "";

    if (!mangas || mangas.length === 0) {
        listContainer.innerHTML = `<p class="text-center text-white/40 uppercase tracking-widest text-xs py-20">Hiện chưa có dữ liệu xếp hạng.</p>`;
        return;
    }

    mangas.forEach((manga, index) => {
        const rank = index + 1;
        let rankClass = "text-white/20";
        if (rank === 1) rankClass = "rank-gold text-5xl";
        else if (rank === 2) rankClass = "rank-silver text-4xl";
        else if (rank === 3) rankClass = "rank-bronze text-3xl";
        else rankClass = "text-white/20 text-2xl";

        const card = document.createElement("a");
        card.href = `detail.html?id=${manga.id}`;
        card.className = "flex items-center gap-8 p-6 bg-white/5 border border-white/5 rounded-2xl hover:bg-white/10 transition-all group animate-fade-up";
        card.style.animationDelay = `${index * 0.1}s`;

        card.innerHTML = `
            <div class="w-16 flex justify-center items-center font-black font-headline ${rankClass}">
                ${rank < 10 ? '0' + rank : rank}
            </div>
            
            <div class="w-24 h-32 shrink-0 rounded-lg overflow-hidden border border-white/10 shadow-xl">
                <img class="w-full h-full object-cover transition-transform group-hover:scale-110 duration-500" 
                     src="${manga.coverImageUrl || 'https://via.placeholder.com/200x300'}" alt="${manga.title}">
            </div>

            <div class="flex-1">
                <div class="flex flex-wrap gap-2 mb-2">
                    <span class="bg-primary/20 text-primary text-[8px] uppercase font-bold tracking-widest px-2 py-1 rounded">
                        ${manga.categoryName || "Manga"}
                    </span>
                    <span class="bg-white/5 text-white/40 text-[8px] uppercase font-bold tracking-widest px-2 py-1 rounded">
                        ${manga.chapters.length} Chương
                    </span>
                </div>
                <h3 class="text-xl md:text-2xl font-black font-headline text-white group-hover:text-primary transition-colors mb-1">
                    ${manga.title}
                </h3>
                <p class="text-xs text-white/40 font-bold uppercase tracking-widest">
                    ${manga.author || "Đang cập nhật"}
                </p>
            </div>

            <div class="hidden md:flex flex-col items-end gap-2 shrink-0">
                <span class="text-primary font-black text-xs uppercase tracking-widest">Xem Ngay</span>
                <span class="material-symbols-outlined text-primary group-hover:translate-x-2 transition-transform">arrow_forward</span>
            </div>
        `;
        listContainer.appendChild(card);
    });
}
