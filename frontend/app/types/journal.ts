export type JournalKey =
  | 'trigger'
  | 'critic'
  | 'emotions'
  | 'behaviors'
  | 'origin'
  | 'reply'
  | 'reframedThought'
  | 'nextAction'
  | 'afterActionEmotion'

export type JournalAnswers = Record<JournalKey, string | string[]>

export type JournalEntry = {
  id: string
  createdAt: string
  criticName: string
  answers: JournalAnswers
  isComplete: boolean
}

export type JournalQuestion = {
  key: JournalKey
  eyebrow: string
  title: string
  help: string
  placeholder?: string
}
