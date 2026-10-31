export type TestType = {
  testTypeId: number
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: number
}

export type UpdateTestTypeRequest = {
  testTypeTitle: string
  testTypeDescription: string
  testTypeFees: number
}
