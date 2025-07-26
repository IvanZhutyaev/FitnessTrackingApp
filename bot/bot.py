import logging
import asyncio
from aiogram import Bot, Dispatcher, types
from aiogram.filters import Command
from aiogram.types import FSInputFile
from gpt4all import GPT4All

API_TOKEN = "8013277160:AAF9UVENmzc4_QvlPN69xt3-b0PiO_EGS0U"
MODEL_PATH = "mistral-7b-instruct-v0.1.Q4_0.gguf"

bot = Bot(token=API_TOKEN)
dp = Dispatcher()

# Инициализация модели (укажи путь к модели правильно)
model = GPT4All(model_name=MODEL_PATH, model_path='.', allow_download=False, backend='cpu')

PROMPT_SUPPORT = """Ты — виртуальный помощник службы поддержки фитнес-приложения. Твоя задача — помогать пользователям быстро и понятно решать проблемы с приложением, отвечать на вопросы о функциях, регистрации, настройках и других аспектах работы сервиса.

Требования к ответам:
- Отвечай кратко, вежливо и уважительно.
- Используй простой и понятный язык, избегай сложных технических терминов.
- Не упоминай, что ты ИИ или бот.
- Отвечай только по теме приложения и его функционала.
- Если вопрос связан с часто задаваемыми вопросами (FAQ), отвечай чётко, используя известные инструкции.
- Если вопрос не ясен или вне твоей компетенции, аккуратно предложи обратиться в поддержку или уточнить вопрос.

Формат ответа:
- Начинай с приветствия, если уместно.
- Отвечай по делу, избегай лишних слов.
- Поддерживай доброжелательный и профессиональный тон.

Пример:

Вопрос: {question}

Ответ:
"""

PROMPT_COACH_QA = """Ты — виртуальный фитнес-тренер, эксперт по тренировкам, питанию, восстановлению и здоровому образу жизни. Твоя задача — мотивировать пользователя и давать точные, полезные советы, которые помогут улучшить физическую форму и общее самочувствие.

Требования к ответам:
- Отвечай чётко и по делу.
- Будь вдохновляющим и поддерживающим, мотивируй к достижению целей.
- Не давай медицинских диагнозов или сложных рекомендаций, если не уверен.
- Отвечай только по теме фитнеса, правильного питания, восстановления и ЗОЖ.
- Избегай длинных и сложных объяснений, говори просто и понятно.
- Не упоминай, что ты ИИ или бот.

Формат ответа:
- Используй дружелюбный и мотивирующий тон.
- Отвечай конкретно на вопрос.
- Если вопрос слишком общий, предложи конкретные шаги или рекомендации.

Пример:

Вопрос: {question}

Ответ:
"""

faq_answers = {
    "Как зарегистрироваться?": "Открой приложение, нажми «Начать» и следуй шагам регистрации.",
    "Как изменить цель?": "В настройках приложения выбери пункт «Цели» и измени параметр.",
    "Как связаться с поддержкой?": "Напиши сюда или используй форму в приложении.",
}

@dp.message(Command("start"))
async def cmd_start(message: types.Message):
    await message.answer("Привет! Я бот техподдержки и фитнес-тренер.\nНапиши /help для списка команд.")

@dp.message(Command("help"))
async def cmd_help(message: types.Message):
    await message.answer("/faq — Частые вопросы\n/ask — Вопрос о приложении\n/coach — Вопрос тренеру\n/app — О приложении")

@dp.message(Command("faq"))
async def cmd_faq(message: types.Message):
    text = "📌 Часто задаваемые вопросы:\n"
    for q, a in faq_answers.items():
        text += f"\n❓ {q}\n💬 {a}\n"
    await message.answer(text)

@dp.message(Command("app"))
async def cmd_app(message: types.Message):
    await message.answer("📲 Наше фитнес-приложение помогает достигать целей, отслеживать тренировки и питаться правильно.")
    photo = FSInputFile("app_preview.jpg")
    await message.answer_photo(photo, caption="Вот как выглядит наше приложение!")

async def generate_response(prompt: str, max_tokens=150, temp=0.5) -> str:
    # Асинхронно запускаем генерацию, чтобы не блокировать event loop
    response = await asyncio.to_thread(model.generate, prompt, max_tokens=max_tokens, temp=temp)
    return response.strip()

@dp.message(Command("ask"))
async def cmd_ask(message: types.Message):
    await message.answer("📩 Напиши свой вопрос по приложению:")

    # Ожидаем следующий ответ пользователя с вопросом
    @dp.message()
    async def process_question(msg: types.Message):
        prompt = PROMPT_SUPPORT.format(question=msg.text)
        thinking_msg = await msg.answer("💬 Печатает...")

        answer = await generate_response(prompt, max_tokens=150, temp=0.4)
        await thinking_msg.edit_text(answer)

        # После обработки вопроса снимаем этот хендлер, чтобы он не реагировал на все сообщения
        dp.message.handlers.unregister(process_question)

@dp.message(Command("coach"))
async def cmd_coach(message: types.Message):
    await message.answer("🏋️ Задай вопрос фитнес-тренеру:")

    @dp.message()
    async def process_coach_question(msg: types.Message):
        prompt = PROMPT_COACH_QA.format(question=msg.text)
        thinking_msg = await msg.answer("💬 Печатает...")

        answer = await generate_response(prompt, max_tokens=150, temp=0.6)
        await thinking_msg.edit_text(f"🏋️ Тренер отвечает:\n{answer}")

        dp.message.handlers.unregister(process_coach_question)

async def main():
    logging.basicConfig(level=logging.INFO)
    await dp.start_polling(bot)

if __name__ == "__main__":
    asyncio.run(main())
