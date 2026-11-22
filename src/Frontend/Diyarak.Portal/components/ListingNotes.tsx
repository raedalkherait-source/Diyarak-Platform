"use client";

import { useEffect, useState } from "react";

const STORAGE_KEY = "diyarak_notes";

interface NotesMap {
  [listingId: string]: string;
}

function loadAll(): NotesMap {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function saveAll(notes: NotesMap) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(notes));
  } catch { /* ignore */ }
}

export function ListingNotes({ listingId }: { listingId: string }) {
  const [note, setNote] = useState("");
  const [saved, setSaved] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const all = loadAll();
    const existing = all[listingId] ?? "";
    setNote(existing);
    if (existing) setOpen(true);
  }, [listingId]);

  function save() {
    const all = loadAll();
    if (note.trim()) {
      all[listingId] = note.trim();
    } else {
      delete all[listingId];
    }
    saveAll(all);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  function remove() {
    const all = loadAll();
    delete all[listingId];
    saveAll(all);
    setNote("");
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  return (
    <section className="panel listing-notes">
      <button type="button" className="notes-toggle" onClick={() => setOpen(!open)}>
        {open ? "▾" : "▸"} ملاحظاتي الخاصة
        {!open && note ? <span className="notes-indicator">●</span> : null}
      </button>
      {open ? (
        <div className="notes-body">
          <textarea
            value={note}
            onChange={(e) => { setNote(e.target.value); setSaved(false); }}
            placeholder="اكتب ملاحظاتك حول هذا الإعلان (لن تُشارك مع أحد)..."
            rows={3}
            maxLength={1000}
          />
          <div className="notes-actions">
            <button type="button" className="button small" onClick={save}>حفظ</button>
            {note ? <button type="button" className="button secondary small" onClick={remove}>حذف</button> : null}
            {saved ? <span className="success">تم الحفظ</span> : null}
          </div>
        </div>
      ) : null}
    </section>
  );
}
