import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "تواصل معنا",
  description: "تواصل مع فريق ديارك للاستفسارات والملاحظات.",
};

export default function ContactPage() {
  return (
    <div className="container section">
      <h1>تواصل معنا</h1>
      <p>يسعدنا تلقي ملاحظاتكم واستفساراتكم. يمكنكم التواصل معنا عبر الطرق التالية:</p>

      <div className="contact-grid">
        <section className="panel contact-card">
          <h2>البريد الإلكتروني</h2>
          <p>للاستفسارات العامة والدعم الفني:</p>
          <a href="mailto:support@diyarak.app" className="contact-link">support@diyarak.app</a>
        </section>

        <section className="panel contact-card">
          <h2>واتساب</h2>
          <p>للتواصل السريع:</p>
          <a href="https://wa.me/message" className="contact-link button secondary">مراسلة عبر واتساب</a>
        </section>

        <section className="panel contact-card">
          <h2>الملاحظات والاقتراحات</h2>
          <p>نرحب بأي ملاحظة أو اقتراح لتحسين المنصة. أرسل لنا رسالة وسنرد في أقرب وقت.</p>
        </section>
      </div>

      <section className="panel" style={{ marginBlockStart: 24 }}>
        <h2>نموذج التواصل</h2>
        <ContactForm />
      </section>
    </div>
  );
}

function ContactForm() {
  return (
    <form className="contact-form" action="mailto:support@diyarak.app" method="post" encType="text/plain">
      <label className="field">
        الاسم
        <input type="text" name="name" required maxLength={100} />
      </label>
      <label className="field">
        البريد الإلكتروني أو رقم الهاتف
        <input type="text" name="contact" required maxLength={100} />
      </label>
      <label className="field">
        الموضوع
        <select name="subject">
          <option value="inquiry">استفسار عام</option>
          <option value="support">دعم فني</option>
          <option value="suggestion">اقتراح</option>
          <option value="complaint">شكوى</option>
          <option value="advertising">إعلان أو شراكة</option>
        </select>
      </label>
      <label className="field">
        الرسالة
        <textarea name="message" rows={5} required maxLength={2000} />
      </label>
      <button type="submit" className="button">إرسال</button>
    </form>
  );
}
