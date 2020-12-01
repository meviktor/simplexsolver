import { TestBed } from '@angular/core/testing';

import { LpsolverService } from './lpsolver.service';

describe('LpsolverService', () => {
  let service: LpsolverService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LpsolverService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
