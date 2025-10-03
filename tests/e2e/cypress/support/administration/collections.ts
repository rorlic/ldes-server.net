/// <reference types="cypress" />

import { Quad_Graph } from 'n3';
import { UrlResponse, rdf, ldes } from '../ldes';

export class CollectionsResponse extends UrlResponse {
  get collections(): Quad_Graph[] {
      return this.store.getGraphs(null, rdf.type, ldes.EventStream);
  }

  get collectionNames(): string[] {
      return this.collections.map(x => x.value.split('/').reverse().shift());
  }

  expectNone(): any {
    expect(this.collections.length).to.equal(0);
  }

  expectAllDefined(collectionNames: string[]): any {
    const collections = this.collections;
    expect(collections.length).to.equal(collectionNames.length);
    expect(this.collectionNames).to.eql(collectionNames);
  }

}