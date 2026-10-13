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
  | 'recordedAt'
  | 'actionStartedAt'
  | 'progressCompletedAt'

export type JournalAnswers = Record<JournalKey, string | string[]>

export type JournalEntry = {
  id: string
  createdAt: string
  updatedAt: string
  criticName: string
  answers: JournalAnswers
  isComplete: boolean
}

export type JournalSort = 'updated-desc' | 'updated-asc' | 'created-desc' | 'created-asc'

export type JournalQuestion = {
  key: JournalKey
  eyebrow: string
  title: string
  help: string
  placeholder?: string
}
