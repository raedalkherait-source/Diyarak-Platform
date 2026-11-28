"use client";

import { useActionState } from "react";
import Link from "next/link";
import { useEffect, useState } from "react";
import {
  createOffPlanProject,
  toggleOffPlanPublish,
  deleteOffPlanProject,
  type OffPlanProjectFormState,
} from "../actions";
import { governorateLabels, constructionStatusLabels } from "@/lib/labels";

interface ManagedProject {
  id: string;
  version: number;
  name: string;
  description: string | null;
  governorate: string;
  district: string;
  constructionStatus: string;
  totalUnits: number;
  minPriceUsd: number | null;
  maxPriceUsd: number | null;
  expectedCompletion: string | null;
  isPublished: boolean;
  createdAt: string;
}

export default function MyProjectsPage() {
  const [projects, setProjects] = useState<ManagedProject[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);

  useEffect(() => {
    fetch("/api/market/offplan", { credentials: "include" })
      .then(async (r) => {
        if (r.ok) {
          const data = (await r.json()) as { items: ManagedProject[] };
          setProjects(data.items);
        }
      })
      .finally(() => setLoading(false));
  }, []);

  const [createState, createAction, createPending] = useActionState<OffPlanProjectFormState, FormData>(
    createOffPlanProject,
    {},
  );

  return (
    <div className="container section">
      <div className="section-title">
        <h1>مشاريعي على المخطط</h1>
        <Link className="button secondary" href="/my">العودة</Link>
      </div>

      <button type="button" className="button toggle-form-btn" onClick={() => setShowForm(!showForm)}>
        {showForm ? "إخفاء النموذج" : "مشروع جديد"}
      </button>

      {showForm && (
        <form action={createAction} className="form-stack">
          <ProjectFormFields />
          <button className="button" type="submit" disabled={createPending}>
            {createPending ? "جاري الحفظ..." : "إنشاء المشروع"}
          </button>
          {createState.status === "error" && <p className="form-error" role="alert">{createState.message}</p>}
        </form>
      )}

      {loading ? (
        <p>جاري التحميل...</p>
      ) : projects.length === 0 ? (
        <p>لا توجد مشاريع. أنشئ مشروعك الأول.</p>
      ) : (
        <div className="card-grid">
          {projects.map((project) => (
            <div key={project.id} className="card">
              <h3>{project.name}</h3>
              <p className="card-meta">
                {governorateLabels[project.governorate] ?? project.governorate} — {project.district}
              </p>
              <p className="card-meta">
                {constructionStatusLabels[project.constructionStatus] ?? project.constructionStatus}
                {" · "}{project.totalUnits} وحدة
              </p>
              <p className="card-meta">
                {project.isPublished ? "منشور" : "مسودة"}
              </p>
              <div className="card-actions">
                <button
                  className={`button small ${project.isPublished ? "secondary" : ""}`}
                  onClick={() => toggleOffPlanPublish(project.id, project.version, !project.isPublished)}
                >
                  {project.isPublished ? "إلغاء النشر" : "نشر"}
                </button>
                {!project.isPublished && (
                  <button
                    className="button small danger"
                    onClick={() => {
                      if (confirm("هل أنت متأكد من حذف هذا المشروع؟")) {
                        deleteOffPlanProject(project.id);
                      }
                    }}
                  >
                    حذف
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function ProjectFormFields() {
  return (
    <>
      <label>
        اسم المشروع
        <input type="text" name="name" required minLength={3} maxLength={120} />
      </label>
      <label>
        الوصف
        <textarea name="description" rows={4} maxLength={4000} />
      </label>
      <label>
        المحافظة
        <select name="governorate" required>
          <option value="">اختر المحافظة</option>
          {Object.entries(governorateLabels).map(([key, label]) => (
            <option key={key} value={key}>{label}</option>
          ))}
        </select>
      </label>
      <label>
        المنطقة / الحي
        <input type="text" name="district" required maxLength={100} />
      </label>
      <label>
        حالة البناء
        <select name="constructionStatus" required>
          {Object.entries(constructionStatusLabels).map(([key, label]) => (
            <option key={key} value={key}>{label}</option>
          ))}
        </select>
      </label>
      <label>
        عدد الوحدات
        <input name="totalUnits" inputMode="numeric" maxLength={6} required />
      </label>
      <label>
        الحد الأدنى للسعر (USD)
        <input name="minPriceUsd" inputMode="decimal" maxLength={20} />
      </label>
      <label>
        الحد الأعلى للسعر (USD)
        <input name="maxPriceUsd" inputMode="decimal" maxLength={20} />
      </label>
      <label>
        تاريخ التسليم المتوقع
        <input type="date" name="expectedCompletion" />
      </label>
    </>
  );
}
